namespace SharedContracts;

public static class StockOperationRules
{
    public static int Available(int total, int reserved)
    {
        return total - reserved;
    }

    public static bool CanMoveOrReserve(int total, int reserved, int quantity)
    {
        return quantity > 0 && Available(total, reserved) >= quantity;
    }

    public static bool CanRelease(int reserved, int quantity)
    {
        return quantity > 0 && reserved >= quantity;
    }

    public static bool CanAdjust(int newTotal, int reserved)
    {
        return newTotal >= 0 && newTotal >= reserved;
    }
}
