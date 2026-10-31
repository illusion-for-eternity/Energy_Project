using Energy_Project.Services.Interfaces;
using Moq;
using Energy_Project.Models;
using Energy_Project.Services;
namespace SmartHomeTests
{
    /// <summary>
    /// Unit tests for EnergyMonitorService.
    /// Tests current energy usage calculations, overload detection,
    /// ensures repository and notification methods are called correctly,
    /// and verifies energy limit updates.
    /// </summary>
    public class EnergyMonitorServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepo = new();
        private readonly Mock<IEnergyPlanRepository> _planRepo = new();
        private readonly Mock<INotificationService> _notify = new();

        /// <summary>
        /// Helper method to create an EnergyMonitorService with mocked dependencies.
        /// Reduces repeated setup code in each test.
        /// </summary>
        /// <returns>Instance of EnergyMonitorService using mocked repositories and notification service</returns>
        private EnergyMonitorService CreateService()
        {
            return new EnergyMonitorService(_deviceRepo.Object, _planRepo.Object, _notify.Object);
        }

        #region CalculateCurrentUsageKwh

        /// <summary>
        /// Test: Calculating energy usage for an active device should return the expected result.
        /// </summary>
        [Fact]
        public void CalculateCurrentUsageKwh_ListWithActiveDevice_ReturnResultEqualToExpected()
        {
            // Arrange
            var service = CreateService();
            const double EXPECTED_USAGE = 0.001;
            var fakeDevicesList = new List<Device>()
            {
                new Device() { IsOn = true, PowerUsageWatts = 1 }
            };

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);

            // Act
            var actual = service.CalculateCurrentUsageKwh();

            // Assert
            Assert.Equal(EXPECTED_USAGE, actual);
        }

        /// <summary>
        /// Test: Calculating energy usage for an inactive device should not return the specified non-zero value.
        /// </summary>
        [Fact]
        public void CalculateCurrentUsageKwh_ListWithUnactiveDevice_ReturnResultNotEqualToExpected()
        {
            // Arrange
            var service = CreateService();
            const double ABNORMAL_USAGE = 0.001;
            var fakeDevicesList = new List<Device>()
            {
                new Device() { IsOn = false, PowerUsageWatts = 1 }
            };

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);

            // Act
            var actual = service.CalculateCurrentUsageKwh();

            // Assert
            Assert.NotEqual(ABNORMAL_USAGE, actual);
        }

        /// <summary>
        /// Test: Calculating energy usage for an empty device list should return zero.
        /// </summary>
        [Fact]
        public void CalculateCurrentUsageKwh_EmptyDevicesList_ReturnsExpectedResult()
        {
            // Arrange
            var service = CreateService();
            var fakeDevicesList = new List<Device>();

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);
            const double EXPECTED_USAGE = 0;

            // Act
            var actual = service.CalculateCurrentUsageKwh();

            // Assert
            Assert.Equal(EXPECTED_USAGE, actual);
        }

        /// <summary>
        /// Test: Calculating energy usage for inactive devices should not return a value greater than zero.
        /// </summary>
        [Fact]
        public void CalculateCurrentUsageKwh_DevicesList_ReturnsNotExpectedResult()
        {
            // Arrange
            var service = CreateService();
            const double MINIMUM_USAGE = 0;
            var fakeDevicesList = new List<Device>()
            {
                new Device() { IsOn = false, PowerUsageWatts = 10 },
                new Device() { IsOn = false, PowerUsageWatts = 20 }
            };

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);

            // Act
            var actual = service.CalculateCurrentUsageKwh();

            // Assert
            Assert.False(actual > MINIMUM_USAGE);
        }

        /// <summary>
        /// Test: Calculating current energy usage should call repository.GetAll() exactly once.
        /// </summary>
        [Fact]
        public void CalculateCurrentUsageKwh_CallsRepositoryGetAllOnce()
        {
            // Arrange
            var service = CreateService();
            var fakeDevicesList = new List<Device>();

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);

            // Act
            service.CalculateCurrentUsageKwh();

            // Assert
            _deviceRepo.Verify(g => g.GetAll(), Times.Once);
        }

        #endregion

        #region CheckForOverload

        /// <summary>
        /// Test: Detecting an overload should call notificationService.SendAlert() exactly once.
        /// </summary>
        [Fact]
        public void CheckForOverload_OverloadPresent_CallsNotifyServiceOnce()
        {
            // Arrange
            var service = CreateService();
            const double DAILY_LIMIT = 10;
            const double DEVICE_POWER = 20000;
            var fakeDevicesList = new List<Device>()
            {
                new Device() { IsOn = true, PowerUsageWatts = DEVICE_POWER }
            };
            var currentPlan = new EnergyPlan() { DailyLimitKwh = DAILY_LIMIT };

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);
            _planRepo.Setup(g => g.GetCurrentPlan()).Returns(currentPlan);

            // Act
            service.CheckForOverload();

            // Assert
            _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test: Checking energy usage below the limit should never call notificationService.SendAlert().
        /// </summary>
        [Fact]
        public void CheckForOverload_OverloadNotPresent_NeverCallsNotifyService()
        {
            // Arrange
            var service = CreateService();
            const double DAILY_LIMIT = 10;
            const double DEVICE_POWER = 2000;
            var currentPlan = new EnergyPlan() { DailyLimitKwh = DAILY_LIMIT };
            var fakeDevicesList = new List<Device>()
            {
                new Device() { IsOn = true, PowerUsageWatts = DEVICE_POWER }
            };

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);
            _planRepo.Setup(g => g.GetCurrentPlan()).Returns(currentPlan);

            // Act
            service.CheckForOverload();

            // Assert
            _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test: Checking energy usage equal to the limit should never call notificationService.SendAlert().
        /// </summary>
        [Fact]
        public void CheckForOverload_OverloadEqualToPowerUsage_NeverCallsNotifyService()
        {
            // Arrange
            var service = CreateService();
            const double DAILY_LIMIT = 10;
            const double DEVICE_POWER = 10000;
            var currentPlan = new EnergyPlan() { DailyLimitKwh = DAILY_LIMIT };

            _deviceRepo.Setup(g => g.GetAll()).Returns(new List<Device>()
            {
                new Device() { IsOn = true, PowerUsageWatts = DEVICE_POWER }
            });

            _planRepo.Setup(g => g.GetCurrentPlan()).Returns(currentPlan);

            // Act
            service.CheckForOverload();

            // Assert
            _notify.Verify(n => n.SendAlert(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region UpdateEnergyLimit

        /// <summary>
        /// Parameterized test for UpdateEnergyLimit method.
        /// Each InlineData provides a new energy limit to verify that repository.UpdatePlan()
        /// is called exactly once.
        /// </summary>
        /// <param name="newLimit">New energy limit to set</param>
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(-2)]
        public void UpdateEnergyLimit_CallsRepositoryTimesOnce(int newLimit)
        {
            // Arrange
            var service = CreateService();
            const double INITIAL_LIMIT = 1;
            var currentPlan = new EnergyPlan() { DailyLimitKwh = INITIAL_LIMIT };

            _planRepo.Setup(g => g.GetCurrentPlan()).Returns(currentPlan);

            // Act
            service.UpdateEnergyLimit(newLimit);

            // Assert
            _planRepo.Verify(u => u.UpdatePlan(It.IsAny<EnergyPlan>()), Times.Once);
        }

        /// <summary>
        /// Test: Updating an energy plan with the same limit should call repository.UpdatePlan() exactly once.
        /// </summary>
        [Fact]
        public void UpdateEnergyLimit_EnergyPlanWithTheSameDailyLimitKwh_CallsRepositoryUpdatePlanOnce()
        {
            // Arrange
            var service = CreateService();

            const double DAILY_LIMIT = 1;
            const double NEW_LIMIT = 1;
            var currentPlan = new EnergyPlan() { DailyLimitKwh = DAILY_LIMIT };

            _planRepo.Setup(g => g.GetCurrentPlan()).Returns(currentPlan);

            // Act
            service.UpdateEnergyLimit(NEW_LIMIT);

            // Assert
            _planRepo.Verify(u => u.UpdatePlan(It.Is<EnergyPlan>(e => e.DailyLimitKwh == DAILY_LIMIT)), Times.Once);
        }

        /// <summary>
        /// Test: Updating an energy plan with default properties should call repository.UpdatePlan() exactly once.
        /// </summary>
        [Fact]
        public void UpdateEnergyLimit_EnergyPlanWithDefaultProperties_CallsRepositoryUpdatePlanOnce()
        {
            // Arrange
            var service = CreateService();
            var currentPlan = new EnergyPlan();

            const double NEW_LIMIT = 1;

            _planRepo.Setup(g => g.GetCurrentPlan()).Returns(currentPlan);

            // Act
            service.UpdateEnergyLimit(NEW_LIMIT);

            // Assert
            _planRepo.Verify(u => u.UpdatePlan(It.IsAny<EnergyPlan>()), Times.Exactly(1));
        }

        /// <summary>
        /// Test: Updating an energy limit should pass a plan with the expected new limit to repository.UpdatePlan().
        /// </summary>
        [Fact]
        public void UpdateEnergyLimit_NewLimit_UpdatesPlanWithExpectedLimit()
        {
            // Arrange
            var service = CreateService();
            const double INITIAL_LIMIT = 1;
            const double NEW_LIMIT = 5;
            var currentPlan = new EnergyPlan { DailyLimitKwh = INITIAL_LIMIT };

            _planRepo.Setup(r => r.GetCurrentPlan()).Returns(currentPlan);

            // Act
            service.UpdateEnergyLimit(NEW_LIMIT);

            // Assert
            _planRepo.Verify(r => r.UpdatePlan(It.Is<EnergyPlan>(p => p.DailyLimitKwh == NEW_LIMIT)), Times.Once);
        }

        #endregion

    }
}
