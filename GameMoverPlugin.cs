using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using GameMover.Views;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace GameMover
{
    public class GameMoverPlugin : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

        public GameMoverPlugin(IPlayniteAPI api) : base(api)
        {
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            yield return new SidebarItem
            {
                Title = "Game Mover",
                Type = SiderbarItemType.View,
                Icon = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon.png"),
                Opened = () => new GameMoverView(PlayniteApi)
            };
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                Description = "Abrir Game Mover",
                MenuSection = "@Game Mover",
                Action = (menuArgs) =>
                {
                    PlayniteApi.MainView.SwitchToLibraryView();
                }
            };
        }
    }
}
