using System;
using System.Text;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using TerrariaApi.Server;
using Terraria.Localization;
using TShockAPI;
namespace CDPlugin
{
    [ApiVersion(2, 1)]
    public class CDPlugin : TerrariaPlugin
    {
        public override string Author => "Ryouun";
        public override string Description => "Drop item when player die.";
        public override string Name => "CD";
        public override Version Version => new Version(1, 0, 0, 0);
        // 物品栏掉落的物品不销毁的概率
        private const int InventoryDDR = 50;
        // 物品栏掉落物品的概率
        private const int InventoryDR = 50;
        // 装备栏掉落的物品不销毁的概率
        private const int ArmDDR = 80;
        // 装备栏掉落物品的概率
        private const int ArmDR = 20;
        // 扩展栏掉落的物品不销毁的概率
        private const int MEDDR = 50;
        // 扩展栏掉落物品的概率
        private const int MEDR = 33;

        public CDPlugin(Main game) : base(game)
        {

        }
        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, NetHooks_GetData);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, NetHooks_GetData);
            }
            base.Dispose(disposing);
        }

        private void NetHooks_GetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.PlayerDeathV2)
            {
                args.Msg.reader.BaseStream.Position = args.Index;
                var playerId = args.Msg.whoAmI;
                var p = Main.player[playerId];
                var deathReason = "";
                using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                {
                    reader.ReadByte();
                    var reason = PlayerDeathReason.FromReader(reader);
                    deathReason = Convert.ToString(reason.GetDeathText(p.name));
                    if (!TShock.ServerSideCharacterConfig.Enabled)
                    {
                        return;
                    }
                    var notice = new StringBuilder();
                    DDrop(Main.player[playerId], Main.player[playerId].inventory, notice, 0, InventoryDDR, InventoryDR);
                    DDrop(Main.player[playerId], Main.player[playerId].armor, notice, 59, ArmDDR, ArmDR);
                    DDrop(Main.player[playerId], Main.player[playerId].miscEquips, notice, 89, MEDDR, MEDR);
                }
            }
        }

        public void DDrop(Player p, Item[] inv, StringBuilder notice, int os, int dontdestroyrate, int droprate)
        {
            for (var i = 0; i < inv.Length; i++)
            {
                if (Main.rand.Next(100) <= droprate)
                {
                    if (inv[i].Name != "")
                    {
                        DropItem(p, inv[i], os + i, droprate);
                    }
                }
            }
        }

        void DropItem(Player p, Item i, int itemID, int droprate)
        {
            if(Main.rand.Next(100) <= droprate)
            {
                var id = Item.NewItem(p.position, i.width, i.height, i.type, i.stack, true, i.prefix, true);
            }
            i.stack = i.type = i.netID = 0;
            i.active = false;
            NetMessage.SendData(5, -1, -1, null, p.whoAmI, itemID, i.prefix);
        }
    }
}
