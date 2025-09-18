using System;
using ChemGa;
using ChemGa.Core.Common.Utils;
using Discord.Commands;

namespace ChemGa.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireDevAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
    {
        try
        {
            if (RequireExHelpers.IsBypassed(services, context.User.Id)) return Task.FromResult(PreconditionResult.FromSuccess());

            if (GlobalDev.DevIds != null && GlobalDev.DevIds.Length > 0)
            {
                if (GlobalDev.DevIds.Contains(context.User.Id)) return Task.FromResult(PreconditionResult.FromSuccess());
                return Task.FromResult(PreconditionResult.FromError("Chỉ các nhà phát triển mới có thể sử dụng lệnh này."));
            }

            // If no dev IDs configured, deny by default (safer).
            return Task.FromResult(PreconditionResult.FromError("Chỉ các nhà phát triển mới có thể sử dụng lệnh này."));
        }
        catch
        {
            return Task.FromResult(PreconditionResult.FromError("Không thể xác thực quyền truy cập."));
        }
    }
}
