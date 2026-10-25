using Energy_Project.Services.Interfaces;
using Moq;
using Energy_Project.Models;
using Energy_Project.Services;
namespace SmartHomeTests
{
    public class EnergyMonitorServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepo = new();
        private readonly Mock<IEnergyPlanRepository> _planRepo = new();
        private readonly Mock<INotificationService> _notify = new();

        public EnergyMonitorService CreateService()
        {
            return new EnergyMonitorService(_deviceRepo.Object, _planRepo.Object, _notify.Object);
        }

        #region CalculateCurrentUsageKwh

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
            Assert.True(EXPECTED_USAGE == actual);
        }

        [Fact] //Assert.False(condition)
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

        [Fact] //Verify()
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

        [Fact]
        public void CheckForOverload_OverloadPresent_CallsRepositoryNotifyOnce()
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

        [Fact]
        public void CheckForOverload_OverloadNotPresent_NeverCallsRepositoryNotify()
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

        [Fact]
        public void CheckForOverload_OverloadEqualToPowerUsage_NeverCallsRepositoryNotify()
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

        [Theory]
        [InlineData(0)]
        [InlineData(999)]
        [InlineData(-999)]
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
