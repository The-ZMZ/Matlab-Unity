using System.Collections.Generic;
using CleanMyRoom.Gameplay.Activities;
using NUnit.Framework;

namespace CleanMyRoom.Tests
{
    [Category("Issue21")]
    public sealed class Issue21_ColorSortingIdsTests
    {
        [Test]
        public void ColorActivityDefinesTwelveUniqueItemIds()
        {
            string[] expected =
            {
                "apple", "carrot", "duck", "leaf", "blue_car", "grapes",
                "strawberry", "basketball", "star", "frog", "balloon", "butterfly"
            };

            Assert.AreEqual(expected, ColorSortingIds.Items.All);
            Assert.AreEqual(12, UniqueCount(ColorSortingIds.Items.All));
        }

        [Test]
        public void ColorActivityDefinesSixUniqueBasketIds()
        {
            string[] expected =
            {
                "basket_red", "basket_orange", "basket_yellow",
                "basket_green", "basket_blue", "basket_purple"
            };

            Assert.AreEqual(expected, ColorSortingIds.Baskets.All);
            Assert.AreEqual(6, UniqueCount(ColorSortingIds.Baskets.All));
        }

        [Test]
        public void ColorSortingConstantsAreStableProtocolValues()
        {
            Assert.AreEqual("apple", ColorSortingIds.Items.Apple);
            Assert.AreEqual("blue_car", ColorSortingIds.Items.BlueCar);
            Assert.AreEqual("butterfly", ColorSortingIds.Items.Butterfly);
            Assert.AreEqual("basket_red", ColorSortingIds.Baskets.Red);
            Assert.AreEqual("basket_purple", ColorSortingIds.Baskets.Purple);
        }

        private static int UniqueCount(string[] values)
        {
            HashSet<string> unique = new HashSet<string>();
            for (int i = 0; i < values.Length; i++)
            {
                unique.Add(values[i]);
            }

            return unique.Count;
        }
    }
}
