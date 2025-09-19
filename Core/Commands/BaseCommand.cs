using System.Reflection;
using ChemGa.Core.Common.Attributes;
using ChemGa.Core.Handler;
using Discord;
using Discord.Commands;
using ChemGa.Interfaces;
using Serilog;

namespace ChemGa.Core.Commands;

public abstract class BaseCommand : ModuleBase<SocketCommandContext>
{
    private static readonly TimeSpan DefaultTempTtl = TimeSpan.FromSeconds(5);
    protected BaseCommand()
    {
        var type = GetType();
        var classMeta = type.GetCustomAttribute<CommandMetaAttribute>();

        var method = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.GetCustomAttribute<CommandAttribute>() != null);

        var discordCmd = method?.GetCustomAttribute<CommandAttribute>();
        var name = discordCmd?.Text ?? type.Name.ToLowerInvariant();
        var description = method?.GetCustomAttribute<SummaryAttribute>()?.Text ?? string.Empty;
        var aliases = new List<string>();
        if (method != null)
        {
            foreach (var a in method.GetCustomAttributes<AliasAttribute>())
                aliases.AddRange(a.Aliases ?? []);
        }
        foreach (var a in type.GetCustomAttributes<AliasAttribute>())
            aliases.AddRange(a.Aliases ?? []);

        var category = type.Name switch
        {
            var s when s.EndsWith("Module", StringComparison.OrdinalIgnoreCase) => s[..^6].ToTitleCase(),
            var s when !string.IsNullOrWhiteSpace(s) => s.ToTitleCase(),
            _ => "General"
        };
        // collect cooldown: take the longer (max) of class and method
        var classCd = type.GetCustomAttribute<CooldownAttribute>()?.Seconds ?? 0;
        var methodCd = method?.GetCustomAttribute<CooldownAttribute>()?.Seconds ?? 0;
        var cooldown = Math.Max(classCd, methodCd);

        // collect aliases already done
        var aliasArray = aliases.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        // collect remarks (RemarksAttribute) - prefer method then class, concat if both
        string? remarks = null;
        var methodRemarks = method?.GetCustomAttributes(true).FirstOrDefault(a => a.GetType().Name == "RemarksAttribute");
        if (methodRemarks != null)
            remarks = methodRemarks.GetType().GetProperty("Text")?.GetValue(methodRemarks) as string;
        var classRemarks = type.GetCustomAttributes(true).FirstOrDefault(a => a.GetType().Name == "RemarksAttribute");
        if (classRemarks != null)
        {
            var classRem = classRemarks.GetType().GetProperty("Text")?.GetValue(classRemarks) as string;
            if (!string.IsNullOrWhiteSpace(classRem)) remarks = string.IsNullOrWhiteSpace(remarks) ? classRem : remarks + "\n" + classRem;
        }

        // collect priority (take max)
        int classPriority = 0;
        int methodPriority = 0;
        var classPriorityAttr = type.GetCustomAttributes(true).FirstOrDefault(a => a.GetType().Name == "PriorityAttribute");
        if (classPriorityAttr != null) classPriority = (int)(classPriorityAttr.GetType().GetProperty("Priority")?.GetValue(classPriorityAttr) ?? 0);
        var methodPriorityAttr = method?.GetCustomAttributes(true).FirstOrDefault(a => a.GetType().Name == "PriorityAttribute");
        if (methodPriorityAttr != null) methodPriority = (int)(methodPriorityAttr.GetType().GetProperty("Priority")?.GetValue(methodPriorityAttr) ?? 0);
        var priority = Math.Max(classPriority, methodPriority);

        // permissions
        var userPerms = new HashSet<GuildPermission>();
        var botPerms = new HashSet<GuildPermission>();

        object[] allAttrs = type.GetCustomAttributes(true).Cast<object>().Concat(method?.GetCustomAttributes(true).Cast<object>() ?? Enumerable.Empty<object>()).ToArray();

        foreach (var a in allAttrs)
        {
            var at = a.GetType();

            // user permissions - look for properties of type GuildPermission or GuildPermission[]
            foreach (var p in at.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (p.PropertyType == typeof(GuildPermission))
                {
                    var v = p.GetValue(a);
                    if (v is GuildPermission gp) userPerms.Add(gp);
                }
                else if (p.PropertyType == typeof(GuildPermission[]))
                {
                    if (p.GetValue(a) is GuildPermission[] v) foreach (var gp in v) userPerms.Add(gp);
                }
            }

            // bot permissions - some attributes may expose BotPermissions property
            var botProp = at.GetProperty("BotPermissions") ?? at.GetProperty("GuildPermissions");
            if (botProp != null)
            {
                if (botProp.PropertyType == typeof(GuildPermission[]))
                {
                    if (botProp.GetValue(a) is GuildPermission[] v) foreach (var gp in v) botPerms.Add(gp);
                }
                else if (botProp.PropertyType == typeof(GuildPermission))
                {
                    var v = botProp.GetValue(a);
                    if (v is GuildPermission gp) botPerms.Add(gp);
                }
            }
        }

        // RequireOwner
        bool requireOwner = allAttrs.Any(a => a.GetType().Name == "RequireOwnerAttribute");

        // RequiredContexts - find RequireContextAttribute and read Contexts property if present
        var contexts = new List<ContextType>();
        var requireContextAttrs = allAttrs.Where(a => a.GetType().Name == "RequireContextAttribute");
        foreach (var r in requireContextAttrs)
        {
            var prop = r.GetType().GetProperty("Contexts") ?? r.GetType().GetProperty("ContextTypes") ?? r.GetType().GetProperty("Context");
            if (prop != null)
            {
                var val = prop.GetValue(r);
                if (val is IEnumerable<ContextType> vals) contexts.AddRange(vals);
                else if (val is ContextType single) contexts.Add(single);
            }
        }

        // Preconditions: collect attribute type names excluding common ones
        var preconditions = allAttrs.Select(a => a.GetType().Name).Where(n => n.EndsWith("Attribute")).Distinct().ToArray();

        // Required roles: look for attributes exposing Roles/RoleNames property
        var requiredRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in allAttrs)
        {
            var at = a.GetType();
            var roleProp = at.GetProperty("Roles") ?? at.GetProperty("RoleNames") ?? at.GetProperty("Role");
            if (roleProp != null)
            {
                var val = roleProp.GetValue(a);
                if (val is string s) requiredRoles.Add(s);
                else if (val is IEnumerable<string> ss) foreach (var r in ss) requiredRoles.Add(r);
            }
        }

        // Groups: collect GroupAttribute values from class and method
        var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in allAttrs)
        {
            var at = a.GetType();
            if (at.Name == "GroupAttribute")
            {
                // try common property names
                var nameProp = at.GetProperty("Text") ?? at.GetProperty("Name") ?? at.GetProperty("Group") ?? at.GetProperty("Prefix");
                if (nameProp != null)
                {
                    var v = nameProp.GetValue(a);
                    if (v is string s) groups.Add(s);
                }
                else
                {
                    // Try ToString or check constructor args via named fields
                    var tostring = a.ToString();
                    if (!string.IsNullOrWhiteSpace(tostring)) groups.Add(tostring);
                }
            }
        }

        var userPermArray = userPerms.Count > 0 ? userPerms.ToArray() : null;
        var botPermArray = botPerms.Count > 0 ? botPerms.ToArray() : null;

        var _commandInfo = new CommandMetadata(
            name,
            description,
            aliasArray,
            category,
            type,
            method,
            cooldown,
            userPermArray,
            botPermArray,
            remarks,
            priority,
            contexts.ToArray(),
            requireOwner,
            preconditions,
            requiredRoles.ToArray()
            ,
            groups.Count > 0 ? groups.ToArray() : null
        );

        CommandMetadataCache.TryAdd(name, _commandInfo);
    }

    /// <summary>
    /// Reply to the current command context with a temporary message that will be deleted after <paramref name="ttl"/>.
    /// Returns the sent message or null if sending failed.
    /// </summary>
    protected async Task<IUserMessage?> TempReplyAsync(string text, TimeSpan ttl)
    {
        try
        {
            if (await ReplyAsync(text).ConfigureAwait(false) is not IUserMessage msg) return null;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(ttl).ConfigureAwait(false);
                    await msg.DeleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete TempReply message");
                }
            });

            return msg;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to send TempReply");
            return null;
        }
    }

    /// <summary>
    /// Send a temporary message to the specified channel that will be deleted after <paramref name="ttl"/>.
    /// Returns the sent message or null if sending failed.
    /// </summary>
    protected async Task<IUserMessage?> TempSendAsync(IMessageChannel channel, string text, TimeSpan ttl)
    {
        ArgumentNullException.ThrowIfNull(channel);
        try
        {
            if (await channel.SendMessageAsync(text).ConfigureAwait(false) is not IUserMessage msg) return null;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(ttl).ConfigureAwait(false);
                    await msg.DeleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete TempSend message");
                }
            });

            return msg;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to send TempSend");
            return null;
        }
    }

    /// <summary>
    /// Reply to the current command context with the same parameters as <see cref="ReplyAsync(string?, bool, Embed?, RequestOptions?)"/> and delete after <paramref name="ttl"/>.
    /// If <paramref name="ttl"/> is omitted a default of 5 seconds will be used.
    /// </summary>
    protected async Task<IUserMessage?> TempReplyAsync(
        string? text = null,
        bool isTTS = false,
        Embed? embed = null,
        RequestOptions? options = null,
        AllowedMentions? allowedMentions = null,
        MessageReference? messageReference = null,
        MessageComponent? components = null,
        TimeSpan ttl = default)
    {
        var effectiveTtl = ttl == default ? DefaultTempTtl : ttl;
        try
        {
            IUserMessage? sent = null;

            // If advanced params are provided, use Channel.SendMessageAsync to pass them through
            if (allowedMentions != null || messageReference != null || components != null)
            {
                if (Context?.Channel is not IMessageChannel ch)
                {
                    Log.Warning("Context channel not available for TempReply (advanced)");
                    return null;
                }

                sent = await ch.SendMessageAsync(text, isTTS, embed, options, allowedMentions, messageReference, components).ConfigureAwait(false);
            }
            else
            {
                sent = await ReplyAsync(text, isTTS, embed, options).ConfigureAwait(false) as IUserMessage;
            }

            if (sent == null) return null;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(effectiveTtl).ConfigureAwait(false);
                    await sent.DeleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete TempReply (overload) message");
                }
            });

            return sent;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to send TempReply (overload)");
            return null;
        }
    }

    /// <summary>
    /// Send a temporary message to the specified channel with the same parameters as <see cref="IMessageChannel.SendMessageAsync(string?, bool, Embed?, RequestOptions?)"/> and delete after <paramref name="ttl"/>.
    /// If <paramref name="ttl"/> is omitted a default of 5 seconds will be used.
    /// </summary>
    protected async Task<IUserMessage?> TempSendAsync(IMessageChannel channel, string? text = null, bool isTTS = false, Embed? embed = null, RequestOptions? options = null, AllowedMentions? allowedMentions = null, MessageReference? messageReference = null, MessageComponent? components = null, TimeSpan ttl = default)
    {
        ArgumentNullException.ThrowIfNull(channel);
        var effectiveTtl = ttl == default ? DefaultTempTtl : ttl;
        try
        {
            if (await channel.SendMessageAsync(text, isTTS, embed, options, allowedMentions, messageReference, components).ConfigureAwait(false) is not IUserMessage sent) return null;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(effectiveTtl).ConfigureAwait(false);
                    await sent.DeleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete TempSend (overload) message");
                }
            });

            return sent;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to send TempSend (overload)");
            return null;
        }
    }

    /// <summary>
    /// Convenience overload that sends a temporary message to the current command context's channel.
    /// </summary>
    protected Task<IUserMessage?> TempSendAsync(string? text = null, bool isTTS = false, Embed? embed = null, RequestOptions? options = null, TimeSpan ttl = default)
    {
        if (Context?.Channel is not IMessageChannel ch) throw new InvalidOperationException("Command context channel is not available or not an IMessageChannel.");
        return TempSendAsync(ch, text, isTTS, embed, options, allowedMentions: null, messageReference: null, components: null, ttl: ttl);
    }

}