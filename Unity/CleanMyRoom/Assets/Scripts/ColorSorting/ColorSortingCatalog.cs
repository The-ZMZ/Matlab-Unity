using System;
using System.Collections.Generic;
using UnityEngine;

namespace CleanMyRoom.ColorSorting
{
    /// <summary>
    /// Presentation catalogue for the Color Sorting activity.
    ///
    /// IMPORTANT: this class describes only how the activity LOOKS (basket
    /// names, colours and object shapes). It never decides whether a placement
    /// is correct and it never contains the object-to-basket answer key. The
    /// answer key lives in MATLAB's Shared_Data/ActivityData/color_sorting.json
    /// and MATLAB is the only authority on correctness.
    /// </summary>
    public static class ColorSortingCatalog
    {
        public const string ActivityId = "COLOR";
        public const int BasketCount = 6;
        public const int ObjectCount = 12;

        /// <summary>Stable basket identifier used on the wire and in the scene.</summary>
        public sealed class BasketInfo
        {
            public readonly string Id;
            public readonly string Label;
            public readonly Color Color;

            public BasketInfo(string id, string label, Color color)
            {
                Id = id;
                Label = label;
                Color = color;
            }
        }

        /// <summary>Stable object identifier used on the wire and in the scene.</summary>
        public sealed class ObjectInfo
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string ColorId;
            public readonly string Shape;

            public ObjectInfo(string id, string label, string colorId, string shape)
            {
                Id = id;
                Label = label;
                ColorId = colorId;
                Shape = shape;
            }
        }

        private static readonly BasketInfo[] Baskets =
        {
            new BasketInfo("basket_red",    "RED",    new Color(0.94f, 0.25f, 0.28f)),
            new BasketInfo("basket_blue",   "BLUE",   new Color(0.20f, 0.48f, 0.92f)),
            new BasketInfo("basket_green",  "GREEN",  new Color(0.24f, 0.76f, 0.38f)),
            new BasketInfo("basket_yellow", "YELLOW", new Color(0.98f, 0.81f, 0.22f)),
            new BasketInfo("basket_orange", "ORANGE", new Color(1.00f, 0.55f, 0.16f)),
            new BasketInfo("basket_purple", "PURPLE", new Color(0.66f, 0.36f, 0.88f))
        };

        // Two differently shaped objects per colour. The colour is the visual
        // cue for the child; the mapping itself is never evaluated in Unity.
        private static readonly ObjectInfo[] Objects =
        {
            new ObjectInfo("object_01", "Red Apple",     "basket_red",    "sphere"),
            new ObjectInfo("object_02", "Red Cube",      "basket_red",    "cube"),
            new ObjectInfo("object_03", "Blue Ball",     "basket_blue",   "sphere"),
            new ObjectInfo("object_04", "Blue Block",    "basket_blue",   "cube"),
            new ObjectInfo("object_05", "Green Ball",    "basket_green",  "sphere"),
            new ObjectInfo("object_06", "Green Cube",    "basket_green",  "cube"),
            new ObjectInfo("object_07", "Yellow Ball",   "basket_yellow", "sphere"),
            new ObjectInfo("object_08", "Yellow Cube",   "basket_yellow", "cube"),
            new ObjectInfo("object_09", "Orange Ball",   "basket_orange", "sphere"),
            new ObjectInfo("object_10", "Orange Cube",   "basket_orange", "cube"),
            new ObjectInfo("object_11", "Purple Ball",   "basket_purple", "sphere"),
            new ObjectInfo("object_12", "Purple Cube",   "basket_purple", "cube")
        };

        /// <summary>
        /// Copy of the presentation basket list. A copy keeps the private
        /// catalogue immutable while still allowing editors/tests to use array
        /// helpers such as <see cref="Array.IndexOf"/>.
        /// </summary>
        public static BasketInfo[] AllBaskets
        {
            get { return (BasketInfo[])Baskets.Clone(); }
        }

        /// <summary>Copy of the presentation object list.</summary>
        public static ObjectInfo[] AllObjects
        {
            get { return (ObjectInfo[])Objects.Clone(); }
        }

        public static BasketInfo GetBasket(string basketId)
        {
            for (int i = 0; i < Baskets.Length; i++)
            {
                if (string.Equals(Baskets[i].Id, basketId, StringComparison.Ordinal))
                {
                    return Baskets[i];
                }
            }
            return null;
        }

        public static ObjectInfo GetObject(string objectId)
        {
            for (int i = 0; i < Objects.Length; i++)
            {
                if (string.Equals(Objects[i].Id, objectId, StringComparison.Ordinal))
                {
                    return Objects[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Visual colour for an object, used only to build its mesh material.
        /// This must not be used to judge an answer.
        /// </summary>
        public static Color VisualColorOf(string objectId)
        {
            ObjectInfo info = GetObject(objectId);
            if (info == null)
            {
                return Color.gray;
            }
            BasketInfo basket = GetBasket(info.ColorId);
            return basket == null ? Color.gray : basket.Color;
        }
    }
}
