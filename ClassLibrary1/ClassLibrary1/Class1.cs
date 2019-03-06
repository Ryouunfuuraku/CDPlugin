using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using TerrariaApi.Server;
using TShockAPI.Hooks;
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
        private const int InventoryDDR = 60;
        // 物品栏掉落物品的概率
        private const int InventoryDR = 5;
        // 装备栏掉落的物品不销毁的概率
        private const int ArmDDR = 95;
        // 装备栏掉落物品的概率
        private const int ArmDR = 5;
        // 扩展栏掉落的物品不销毁的概率
        private const int MEDDR = 95;
        // 扩展栏掉落物品的概率
        private const int MEDR = 33;
        public static TSPlayer[] Players = new TSPlayer[Main.maxPlayers];
        public CDPlugin(Main game) : base(game)
        {

        }
        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, NetHooks_GetData);
            ServerApi.Hooks.GameInitialize.Register(this, OnInit);
            PlayerHooks.PlayerPreLogin += OnLogin;
            PlayerHooks.PlayerLogout += OnLogout;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, NetHooks_GetData);
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInit);
                PlayerHooks.PlayerPreLogin -= OnLogin;
                PlayerHooks.PlayerLogout -= OnLogout;
            }
            base.Dispose(disposing);
        }

        private void OnInit(EventArgs args)
        {
            DB.Connect();
        }
        public void OnLogin(PlayerPreLoginEventArgs args)
        {
            DB.Login(args.Player.User.ID);
        }

        public void OnLogout(PlayerLogoutEventArgs args)
        {
            DB.Logout(args.Player.User.ID, args.Player.TPlayer.position.X, args.Player.TPlayer.position.Y);
        }
        
        private void NetHooks_GetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.PlayerDeathV2)
            {
                args.Msg.reader.BaseStream.Position = args.Index;
                var playerId = args.Msg.whoAmI;
                var tPlayer = TShock.Players[playerId];
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
                    InventoryDrop(Main.player[playerId], Main.player[playerId].inventory, 0, InventoryDDR, InventoryDR);
                    ArmDrop(Main.player[playerId], Main.player[playerId].armor, 59, ArmDDR, ArmDR);
                    MiscItemDrop(Main.player[playerId], Main.player[playerId].miscEquips, 89, MEDDR, MEDR);
                    
                }
            }
            if (args.MsgID == PacketTypes.PlayerUpdate)
            {
                var playerId = args.Msg.whoAmI;
                var tPlayer = TShock.Players[playerId];
                var p = Main.player[playerId];
                if (!TShock.ServerSideCharacterConfig.Enabled && tPlayer.User == null)
                {
                    return;
                }
                var ppos = DB.GetPos(tPlayer.User.ID);
                if (ppos.indatabase)
                {
                    if (tPlayer.Teleport(ppos.x, ppos.y))
                    {
                        DB.UpdateStatus(false, tPlayer.User.ID);
                    }
                }
            }
        }

        public void InventoryDrop(Player p, Item[] inv, int os, int dontdestroyrate, int droprate)
        {
            for (var i = 0; i < inv.Length; i++)
            {
                if (Main.rand.Next(100) <= droprate || i < 10)
                {
                    if (inv[i].Name != "")
                    {
                        DropItem(p, inv[i], os + i, dontdestroyrate);
                    }
                }
            }
        }
        public void ArmDrop(Player p, Item[] inv, int os, int dontdestroyrate, int droprate)
        {

            for (var i = 0; i < inv.Length; i++)
            {
                if (Main.rand.Next(100) <= droprate && (i < 3 || (i > 9 && i < 13)))
                {
                    if (inv[i].Name != "")
                    {
                        DropItem(p, inv[i], os + i, dontdestroyrate);
                    }
                }
            }
        }
        public void MiscItemDrop(Player p, Item[] inv, int os, int dontdestroyrate, int droprate)
        {
            for (var i = 0; i < inv.Length; i++)
            {
                if (Main.rand.Next(100) <= droprate)
                {
                    if (inv[i].Name != "")
                    {
                        DropItem(p, inv[i], os + i, dontdestroyrate);
                    }
                }
            }
        }

        void DropItem(Player p, Item i, int itemID, int ddrate)
        {
            if (Main.rand.Next(100) <= ddrate)
            {
                if (i.stack > 1)
                {
                    var id = Item.NewItem(p.position, i.width, i.height, i.type, (int)Math.Round(i.stack * Main.rand.Next(100) / 100.0), true, i.prefix, true);
                }
                else
                {
                    var id = Item.NewItem(p.position, i.width, i.height, i.type, i.stack, true, i.prefix, true);
                }
            }
            i.stack = i.type = i.netID = 0;
            i.active = false;
            NetMessage.SendData(5, -1, -1, null, p.whoAmI, itemID, i.prefix);
        }
        
        public enum ItemSlot
        {
            InvRow1Slot1, InvRow1Slot2, InvRow1Slot3, InvRow1Slot4, InvRow1Slot5, InvRow1Slot6, InvRow1Slot7, InvRow1Slot8, InvRow1Slot9, InvRow1Slot10,
            InvRow2Slot1, InvRow2Slot2, InvRow2Slot3, InvRow2Slot4, InvRow2Slot5, InvRow2Slot6, InvRow2Slot7, InvRow2Slot8, InvRow2Slot9, InvRow2Slot10,
            InvRow3Slot1, InvRow3Slot2, InvRow3Slot3, InvRow3Slot4, InvRow3Slot5, InvRow3Slot6, InvRow3Slot7, InvRow3Slot8, InvRow3Slot9, InvRow3Slot10,
            InvRow4Slot1, InvRow4Slot2, InvRow4Slot3, InvRow4Slot4, InvRow4Slot5, InvRow4Slot6, InvRow4Slot7, InvRow4Slot8, InvRow4Slot9, InvRow4Slot10,
            InvRow5Slot1, InvRow5Slot2, InvRow5Slot3, InvRow5Slot4, InvRow5Slot5, InvRow5Slot6, InvRow5Slot7, InvRow5Slot8, InvRow5Slot9, InvRow5Slot10,
            CoinSlot1, CoinSlot2, CoinSlot3, CoinSlot4, AmmoSlot1, AmmoSlot2, AmmoSlot3, AmmoSlot4, HandSlot,
            ArmorHeadSlot, ArmorBodySlot, ArmorLeggingsSlot, AccessorySlot1, AccessorySlot2, AccessorySlot3, AccessorySlot4, AccessorySlot5, AccessorySlot6, UnknownSlot1,
            VanityHeadSlot, VanityBodySlot, VanityLeggingsSlot, SocialAccessorySlot1, SocialAccessorySlot2, SocialAccessorySlot3, SocialAccessorySlot4, SocialAccessorySlot5, SocialAccessorySlot6, UnknownSlot2,
            DyeHeadSlot, DyeBodySlot, DyeLeggingsSlot, DyeAccessorySlot1, DyeAccessorySlot2, DyeAccessorySlot3, DyeAccessorySlot4, DyeAccessorySlot5, DyeAccessorySlot6, Unknown3,
            EquipmentSlot1, EquipmentSlot2, EquipmentSlot3, EquipmentSlot4, EquipmentSlot5,
            DyeEquipmentSlot1, DyeEquipmentSlot2, DyeEquipmentSlot3, DyeEquipmentSlot4, DyeEquipmentSlot5
        }
    }
}
