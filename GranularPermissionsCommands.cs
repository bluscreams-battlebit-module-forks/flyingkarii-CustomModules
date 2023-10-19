﻿using BBRAPIModules;
using Commands;
using Permissions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PermissionsManager;

[RequireModule(typeof(CommandHandler))]
[RequireModule(typeof(GranularPermissions))]
[Module("Provide commands for managing GranularPermissions", "1.0.0")]
public class GranularPermissionsCommands : BattleBitModule
{
    [ModuleReference]
    public GranularPermissions GranularPermissions { get; set; } = null!;
    [ModuleReference]
    public CommandHandler CommandHandler { get; set; } = null!;

    public GranularPermissionsCommandsConfiguration Configuration { get; set; } = null!;

    public override void OnModulesLoaded()
    {
        this.CommandHandler.Register(this);
    }

    [CommandCallback("addplayerperm", Description = "Adds a permission to a player", Permissions = new[] { "GranularPermissions.AddPlayerPerm" })]
    public void AddPermissionCommand(Context ctx, RunnerPlayer player, string permission)
    {
        this.GranularPermissions.AddPlayerPermission(player.SteamID, permission);

        ctx.Reply($"Added permission {permission} to {player.Name}");
        
        this.GranularPermissions.Save();
    }

    [CommandCallback("removeplayerperm", Description = "Removes a permission from a player", Permissions = new[] { "GranularPermissions.RemovePlayerPerm" })]
    public void RemovePermissionCommand(Context ctx, RunnerPlayer player, string permission)
    {
        this.GranularPermissions.RemovePlayerPermission(player.SteamID, permission);

        ctx.Reply($"Removed permission {permission} from {player.Name}");

        this.GranularPermissions.Save();
    }

    [CommandCallback("clearplayerperms", Description = "Clears all permissions and groups from a player", Permissions = new[] { "GranularPermissions.ClearPlayerPerms" })]
    public void ClearPermissionCommand(Context ctx, RunnerPlayer player)
    {
        foreach (string group in this.GranularPermissions.GetPlayerGroups(player.SteamID))
        {
            this.GranularPermissions.RemovePlayerGroup(player.SteamID, group);
        }

        foreach (string permission in this.GranularPermissions.GetPlayerPermissions(player.SteamID))
        {
            this.GranularPermissions.RemovePlayerPermission(player.SteamID, permission);
        }

        ctx.Reply($"Cleared permissions from {player.Name}");

        this.GranularPermissions.Save();
    }

    [CommandCallback("listplayerperms", Description = "Lists player permissions", Permissions = new[] { "GranularPermissions.ListPlayerPerms" })]
    public void ListPermissionCommand(Context ctx, RunnerPlayer targetPlayer, int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }

        string[] permissions = this.GranularPermissions.GetAllPlayerPermissions(targetPlayer.SteamID);

        int pageCount = (int)Math.Ceiling(permissions.Length / (double)this.Configuration.PermissionsPerPage);

        ctx.Reply($"{targetPlayer.Name}:{Environment.NewLine}{string.Join("\n", permissions.Skip((page - 1) * this.Configuration.PermissionsPerPage).Take(this.Configuration.PermissionsPerPage))}{(pageCount > 1 ? $"{Environment.NewLine}Page {page} of {pageCount}{(page == pageCount ? "" : $", use listperms \"{targetPlayer.Name}\" {page + 1} to see more")}" : "")}");
    }

    [CommandCallback("addplayergroup", Description = "Adds a group to a player", Permissions = new[] { "GranularPermissions.AddPlayerGroup" })]
    public void AddGroupCommand(Context ctx, RunnerPlayer player, string group)
    {
        if (this.GranularPermissions.GetPlayerGroups(player.SteamID).Contains(group))
        {
            ctx.Reply($"{player.Name} already has group {group}");
            return;
        }

        if (!this.GranularPermissions.GetGroups().Contains(group))
        {
            ctx.Reply($"Group {group} does not exist");
            return;
        }

        this.GranularPermissions.AddPlayerGroup(player.SteamID, group);

        ctx.Reply($"Added group {group} to {player.Name}");

        this.GranularPermissions.Save();
    }

    [CommandCallback("removeplayergroup", Description = "Removes a group from a player", Permissions = new[] { "GranularPermissions.RemovePlayerGroup" })]
    public void RemoveGroupCommand(Context ctx, RunnerPlayer player, string group)
    {
        if (!this.GranularPermissions.GetPlayerGroups(player.SteamID).Contains(group))
        {
            ctx.Reply($"{player.Name} does not have group {group}");
            return;
        }

        this.GranularPermissions.RemovePlayerGroup(player.SteamID, group);

        ctx.Reply($"Removed group {group} from {player.Name}");

        this.GranularPermissions.Save();
    }

    [CommandCallback("clearplayergroups", Description = "Clears all groups from a player", Permissions = new[] { "GranularPermissions.ClearPlayerGroups" })]
    public void ClearGroupCommand(Context ctx, RunnerPlayer player)
    {
        foreach (string group in this.GranularPermissions.GetPlayerGroups(player.SteamID))
        {
            this.GranularPermissions.RemovePlayerGroup(player.SteamID, group);
        }

        ctx.Reply($"Cleared groups from {player.Name}");

        this.GranularPermissions.Save();
    }

    [CommandCallback("listplayergroups", Description = "Lists player groups", Permissions = new[] { "GranularPermissions.ListPlayerGroups" })]
    public void ListGroupCommand(Context ctx, RunnerPlayer targetPlayer, int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }

        List<string> groups = new();

        groups.AddRange(this.GranularPermissions.GetPlayerGroups(targetPlayer.SteamID));

        int pageCount = (int)Math.Ceiling(groups.Count / (double)this.Configuration.PermissionsPerPage);

        ctx.Reply($"{targetPlayer.Name}:{Environment.NewLine}{string.Join("\n", groups.Skip((page - 1) * this.Configuration.PermissionsPerPage).Take(this.Configuration.PermissionsPerPage))}{(pageCount > 1 ? $"{Environment.NewLine}Page {page} of {pageCount}{(page == pageCount ? "" : $", use listgroups \"{targetPlayer.Name}\" {page + 1} to see more")}" : "")}");
    }

    [CommandCallback("addgroup", Description = "Adds a group", Permissions = new[] { "GranularPermissions.AddGroup" })]
    public void AddGroupCommand(Context ctx, string group)
    {
        if (this.GranularPermissions.GetGroups().Contains(group))
        {
            ctx.Reply($"Group {group} already exists");
            return;
        }

        this.GranularPermissions.AddGroup(group);

        ctx.Reply($"Added group {group}");

        this.GranularPermissions.Save();
    }

    [CommandCallback("removegroup", Description = "Removes a group", Permissions = new[] { "GranularPermissions.RemoveGroup" })]
    public void RemoveGroupCommand(Context ctx, string group)
    {
        if (!this.GranularPermissions.GetGroups().Contains(group))
        {
            ctx.Reply($"Group {group} does not exist");
            return;
        }

        this.GranularPermissions.RemoveGroup(group);

        ctx.Reply($"Removed group {group}");

        this.GranularPermissions.Save();
    }

    [CommandCallback("listgroups", Description = "Lists groups", Permissions = new[] { "GranularPermissions.ListGroups" })]
    public void ListGroupCommand(Context ctx, int page = 1)
    {
        if (page < 1)
        {
            page = 1;
        }

        List<string> groups = new();

        groups.AddRange(this.GranularPermissions.GetGroups());

        int pageCount = (int)Math.Ceiling(groups.Count / (double)this.Configuration.PermissionsPerPage);

        ctx.Reply($"{Environment.NewLine}{string.Join("\n", groups.Skip((page - 1) * this.Configuration.PermissionsPerPage).Take(this.Configuration.PermissionsPerPage))}{(pageCount > 1 ? $"{Environment.NewLine}Page {page} of {pageCount}{(page == pageCount ? "" : $", use listgroups {page + 1} to see more")}" : "")}");
    }

    [CommandCallback("addgroupperm", Description = "Adds a permission to a group", Permissions = new[] { "GranularPermissions.AddGroupPerm" })]
    public void AddGroupPermissionCommand(Context ctx, string group, string permission)
    {
        if (!this.GranularPermissions.GetGroups().Contains(group))
        {
            ctx.Reply($"Group {group} does not exist");
            return;
        }

        if (this.GranularPermissions.GetGroupPermissions(group).Contains(permission))
        {
            ctx.Reply($"{group} already has permission {permission}");
            return;
        }

        this.GranularPermissions.AddGroupPermission(group, permission);

        ctx.Reply($"Added permission {permission} to {group}");

        this.GranularPermissions.Save();
    }

    [CommandCallback("removegroupperm", Description = "Removes a permission from a group", Permissions = new[] { "GranularPermissions.RemoveGroupPerm" })]
    public void RemoveGroupPermissionCommand(Context ctx, string group, string permission)
    {
        if (!this.GranularPermissions.GetGroups().Contains(group))
        {
            ctx.Reply($"Group {group} does not exist");
            return;
        }

        if (!this.GranularPermissions.GetGroupPermissions(group).Contains(permission))
        {
            ctx.Reply($"{group} does not have permission {permission}");
            return;
        }

        this.GranularPermissions.RemoveGroupPermission(group, permission);

        ctx.Reply($"Removed permission {permission} from {group}");

        this.GranularPermissions.Save();
    }

    [CommandCallback("cleargroupperms", Description = "Clears all permissions from a group", Permissions = new[] { "GranularPermissions.ClearGroupPerms" })]
    public void ClearGroupPermissionCommand(Context ctx, string group)
    {
        if (!this.GranularPermissions.GetGroups().Contains(group))
        {
            ctx.Reply($"Group {group} does not exist");
            return;
        }

        foreach (string permission in this.GranularPermissions.GetGroupPermissions(group))
        {
            this.GranularPermissions.RemoveGroupPermission(group, permission);
        }

        ctx.Reply($"Cleared permissions from {group}");

        this.GranularPermissions.Save();
    }

    [CommandCallback("listgroupperms", Description = "Lists group permissions", Permissions = new[] { "GranularPermissions.ListGroupPerms" })]
    public void ListGroupPermissionCommand(Context ctx, string group, int page = 1)
    {
        if (!this.GranularPermissions.GetGroups().Contains(group))
        {
            ctx.Reply($"Group {group} does not exist");
            return;
        }

        if (page < 1)
        {
            page = 1;
        }

        List<string> permissions = new();

        permissions.AddRange(this.GranularPermissions.GetGroupPermissions(group));

        int pageCount = (int)Math.Ceiling(permissions.Count / (double)this.Configuration.PermissionsPerPage);

        ctx.Reply($"{group}:{Environment.NewLine}{string.Join("\n", permissions.Skip((page - 1) * this.Configuration.PermissionsPerPage).Take(this.Configuration.PermissionsPerPage))}{(pageCount > 1 ? $"{Environment.NewLine}Page {page} of {pageCount}{(page == pageCount ? "" : $", use listgroupperms \"{group}\" {page + 1} to see more")}" : "")}");
    }
}

public class GranularPermissionsCommandsConfiguration : ModuleConfiguration
{
    public int PermissionsPerPage { get; set; } = 6;
}
