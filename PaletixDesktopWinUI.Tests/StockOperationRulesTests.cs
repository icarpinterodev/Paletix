using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharedContracts;

namespace PaletixDesktopWinUI.Tests
{
    [TestClass]
    public sealed class StockOperationRulesTests
    {
        [TestMethod]
        public void AvailableSubtractsReservedFromTotal()
        {
            Assert.AreEqual(7, StockOperationRules.Available(total: 10, reserved: 3));
        }

        [TestMethod]
        public void MoveOrReserveRequiresPositiveQuantityAndEnoughAvailable()
        {
            Assert.IsTrue(StockOperationRules.CanMoveOrReserve(total: 10, reserved: 3, quantity: 7));
            Assert.IsFalse(StockOperationRules.CanMoveOrReserve(total: 10, reserved: 3, quantity: 8));
            Assert.IsFalse(StockOperationRules.CanMoveOrReserve(total: 10, reserved: 3, quantity: 0));
        }

        [TestMethod]
        public void ReleaseCannotExceedReservedQuantity()
        {
            Assert.IsTrue(StockOperationRules.CanRelease(reserved: 5, quantity: 5));
            Assert.IsFalse(StockOperationRules.CanRelease(reserved: 5, quantity: 6));
            Assert.IsFalse(StockOperationRules.CanRelease(reserved: 5, quantity: 0));
        }

        [TestMethod]
        public void AdjustCannotSetTotalBelowReserved()
        {
            Assert.IsTrue(StockOperationRules.CanAdjust(newTotal: 5, reserved: 5));
            Assert.IsFalse(StockOperationRules.CanAdjust(newTotal: 4, reserved: 5));
            Assert.IsFalse(StockOperationRules.CanAdjust(newTotal: -1, reserved: 0));
        }
    }
}
