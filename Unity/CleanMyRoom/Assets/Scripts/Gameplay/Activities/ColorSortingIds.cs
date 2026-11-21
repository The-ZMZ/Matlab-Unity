namespace CleanMyRoom.Gameplay.Activities
{
    /// <summary>
    /// Protocol identifiers shared with the MATLAB Color Sorting dataset.
    ///
    /// Unity uses these only to attach presentation objects to requests. The
    /// correct item-to-basket mapping is deliberately absent: MATLAB owns it.
    /// </summary>
    public static class ColorSortingIds
    {
        public static class Items
        {
            public const string Apple = "apple";
            public const string Carrot = "carrot";
            public const string Duck = "duck";
            public const string Leaf = "leaf";
            public const string BlueCar = "blue_car";
            public const string Grapes = "grapes";
            public const string Strawberry = "strawberry";
            public const string Basketball = "basketball";
            public const string Star = "star";
            public const string Frog = "frog";
            public const string Balloon = "balloon";
            public const string Butterfly = "butterfly";

            public static readonly string[] All =
            {
                Apple, Carrot, Duck, Leaf, BlueCar, Grapes,
                Strawberry, Basketball, Star, Frog, Balloon, Butterfly
            };
        }

        public static class Baskets
        {
            public const string Red = "basket_red";
            public const string Orange = "basket_orange";
            public const string Yellow = "basket_yellow";
            public const string Green = "basket_green";
            public const string Blue = "basket_blue";
            public const string Purple = "basket_purple";

            public static readonly string[] All =
            {
                Red, Orange, Yellow, Green, Blue, Purple
            };
        }
    }
}
