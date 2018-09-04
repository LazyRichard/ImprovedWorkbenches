using System.Reflection;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(BillStack), "DoListing")]
    public class BillStack_DoListing_Detour
    {
        public static int ReorderableGroup { get; private set; }

        public static bool BlockButtonDraw = false;

        private static readonly FieldInfo WinSizeGetter = typeof(ITab_Bills).GetField("WinSize",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo PasteXGetter = typeof(ITab_Bills).GetField("PasteX",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo PasteYGetter = typeof(ITab_Bills).GetField("PasteY",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo PasteSizeGetter = typeof(ITab_Bills).GetField("PasteSize",
            BindingFlags.NonPublic | BindingFlags.Static);


        public static bool Prefix()
        {
            if (!Main.Instance.ShouldAllowDragToReorder())
                return true;

            var selectedThing = Find.Selector.SingleSelectedThing;
            var billGiver = selectedThing as IBillGiver;
            if (billGiver == null)
                return true;

            if (!(selectedThing is Building_WorkTable) && !Main.Instance.IsOfTypeRimFactoryBuilding(selectedThing))
                return true;

            ReorderableGroup = ReorderableWidget.NewGroup(
                (from, to) => ReorderBillInStack(billGiver.BillStack, from, to),
                ReorderableDirection.Vertical);

            return true;
        }

        static void ReorderBillInStack(BillStack stack, int from, int to)
        {
            if (to >= stack.Count)
                to = stack.Count - 1;

            if (from == to)
                return;

            var bill = stack[from];
            var offset = to - from;
            stack.Reorder(bill, offset);
        }

        public static void Postfix(ref Rect rect)
        {
            if (!(Find.Selector.SingleSelectedThing is Building_WorkTable workTable))
                return;

            var gap = 4f;
            var buttonWidth = 70f;
            var winSize = (Vector2) WinSizeGetter.GetValue(null);
            var pasteX = (float) PasteXGetter.GetValue(null);
            var pasteY = (float) PasteYGetter.GetValue(null);
            var pasteSize = (float) PasteSizeGetter.GetValue(null);
            var rectCopyAll = new Rect(winSize.x - pasteX - gap - pasteSize, pasteY, pasteSize, pasteSize);

            var billCopyPasteHandler = Main.Instance.BillCopyPasteHandler;
            if (workTable.BillStack != null && workTable.BillStack.Count > 0)
            {
                if (Widgets.ButtonImageFitted(rectCopyAll, Resources.CopyButton, Color.white))
                {
                    billCopyPasteHandler.DoCopy(workTable);
                }
                TooltipHandler.TipRegion(rectCopyAll, "IW.CopyAllTip".Translate());
            }

            if (!billCopyPasteHandler.CanPasteInto(workTable))
                return;

            var rectPaste = new Rect(rectCopyAll);
            rectPaste.xMin += buttonWidth + gap;
            rectPaste.xMax += buttonWidth + gap;
            if (Widgets.ButtonText(rectPaste, 
                billCopyPasteHandler.IsMultipleBillsCopied() ? "IW.PasteAllLabel".Translate() : "IW.Paste".Translate()))
            {
                billCopyPasteHandler.DoPasteInto(workTable, false);
            }
            TooltipHandler.TipRegion(rectPaste, "IW.PasteAllTip".Translate());

            var oldFont = Text.Font;
            Text.Font = GameFont.Tiny;

            var rectLink = new Rect(rectPaste);
            rectLink.xMin += buttonWidth + gap;
            rectLink.xMax += buttonWidth + gap;
            if (Widgets.ButtonText(rectLink, "IW.PasteLinkLabel".Translate()))
            {
                billCopyPasteHandler.DoPasteInto(workTable, true);
            }
            TooltipHandler.TipRegion(rectLink,
                "IW.PasteLinkTip".Translate());

            Text.Font = oldFont;
        }
    }
}