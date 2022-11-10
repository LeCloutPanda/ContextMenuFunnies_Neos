using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using NeosModLoader;
using System;

namespace ContextMenuFunnies
{
    public class Patch : NeosMod
    {
        public override string Name => "Context-Menu-Funnies";
        public override string Author => "LeCloutPanda";
        public override string Version => "1.0.0";

        public static ModConfiguration config;

        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> ENABLED = new ModConfigurationKey<bool>("Enabled", "", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> ICON_ENABLED = new ModConfigurationKey<bool>("Selected item icon visibility", "", () => true);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> INNER_CIRCLE_ENABLED = new ModConfigurationKey<bool>("Inner circle visibilty", "", () => true);

        // Ratio/Seperation
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<float> SEPERATION = new ModConfigurationKey<float>("Distance between menu items", "", () => 6f);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<float> RADIUS_RATIO = new ModConfigurationKey<float>("Distance between centre edge of menu item and centre of menu (0 to 1 range)", "", () => 0.5f);
        
        // Scale
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<float> SIZE_MULTIPLIER = new ModConfigurationKey<float>("Menu scale multiplier", "", () => 1f);

        // Custom Icon Stuff
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> CUSTOM_ICON_ENABLED = new ModConfigurationKey<bool>("Custom icon visibility", "", () => false);
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<string> IMAGE_URI = new ModConfigurationKey<string>("Custom icon url", "", () => "");

        public override void OnEngineInit()
        {
            config = GetConfiguration();
            config.Save(true);

            Harmony harmony = new Harmony($"dev.{Author}.{Name}");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(ContextMenu))]
        class ContextMenuPatch
        {
            static Slot iconSlot;
            static StaticTexture2D iconComp;

            [HarmonyPrefix]
            [HarmonyPatch("OnChanges")]
            static void PrefixOnChanges(ContextMenu __instance, Sync<float> ___Separation, Sync<float> ___RadiusRatio, SyncRef<Image> ____iconImage, SyncRef<OutlinedArc> ____innerCircle)
            {
                __instance.RunInUpdates(3, () =>
                {
                    if (!config.GetValue(ENABLED)) return;

                    if (__instance.Slot.ActiveUserRoot.ActiveUser != __instance.LocalUser) return;

                    ___Separation.Value = config.GetValue(SEPERATION);
                    ___RadiusRatio.Value = config.GetValue(RADIUS_RATIO);

                    ____innerCircle.Target.Enabled = config.GetValue(INNER_CIRCLE_ENABLED);
                    ____iconImage.Target.Slot.ActiveSelf = config.GetValue(ICON_ENABLED);

                    iconComp.URL.Value = new Uri(config.GetValue(IMAGE_URI));
                    iconSlot.ActiveSelf_Field.Value = config.GetValue(CUSTOM_ICON_ENABLED);
                });
            }

            [HarmonyPostfix]
            [HarmonyPatch("OnAwake")]
            static void PrefixOnAwake(ContextMenu __instance, SyncRef<OutlinedArc> ____innerCircle)
            {
                __instance.RunInUpdates(3, () =>
                {
                    if (!config.GetValue(ENABLED)) return;

                    if (__instance.Slot.ActiveUserRoot.ActiveUser != __instance.LocalUser) return;

                    Slot visualSlot = __instance.Slot[0];
                    visualSlot.LocalScale = visualSlot.LocalScale * config.GetValue(SIZE_MULTIPLIER);

                    if (config.GetValue(CUSTOM_ICON_ENABLED))
                    {
                        iconSlot = ____innerCircle.Target.Slot.AddSlot("newImage");
                        Image newImageComp = iconSlot.AttachComponent<Image>();
                        SpriteProvider newSpriteComp = newImageComp.Slot.AttachSprite(new Uri(config.GetValue(IMAGE_URI)), true, false, true, null);
                        newImageComp.Sprite.Target = newSpriteComp;
                        iconComp = newSpriteComp.Slot.GetComponent<StaticTexture2D>();
                        newImageComp.Tint.Value = new color(1f);
                    }
                });
            }
        }
    }
}
