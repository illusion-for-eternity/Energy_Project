using Energy_Project.Services.Interfaces;
using Moq;
using Energy_Project.Models;
using Energy_Project.Services;

namespace SmartHomeTests
{
    public class DeviceServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepo = new();

        private DeviceService CreateService()
        {
            return new DeviceService(_deviceRepo.Object);
        }

        #region ToggleDeviceMethod

        [Theory]
        [InlineData(1, true, true)]
        [InlineData(1, false, false)]
        public void ToggleDevice_ReturnsExpectedResult(int deviceId, bool deviceTurnOnSetting, bool expected)
        {
            // Arrange
            var service = CreateService();
            var fakeDevice = new Device() { IsOn = true };

            _deviceRepo.Setup(g => g.GetById(It.IsAny<int>())).Returns(fakeDevice);

            // Act
            var result = service.ToggleDevice(deviceId, deviceTurnOnSetting);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToggleDevice_DeviceWithDefaultProperties_ReturnsIsOnSetToTrue()
        {
            // Arrange
            var service = CreateService();
            const int DEVICE_ID = 1;
            const bool TURN_ON = true;
            var fakeDevice = new Device();

            _deviceRepo.Setup(g => g.GetById(It.IsAny<int>())).Returns(fakeDevice);

            // Act
            var result = service.ToggleDevice(DEVICE_ID, TURN_ON);

            // Assert
            Assert.Equal(TURN_ON, result);
        }

        [Fact]
        public void ToggleDevice_DeviceToOnSetToTrue_ReturnsDifferentIsOnState()
        {
            // Arrange
            var service = CreateService();
            const int DEVICE_ID = 1;
            const bool TURN_ON = true;
            const bool EXPECTED = !TURN_ON;
            var fakeDevice = new Device() { Id = DEVICE_ID, Name = "LED Lamp", PowerUsageWatts = 20, IsOn = false };

            _deviceRepo.Setup(g => g.GetById(It.IsAny<int>())).Returns(fakeDevice);

            // Act
            var result = service.ToggleDevice(DEVICE_ID, TURN_ON);

            // Assert
            Assert.NotEqual(EXPECTED, result);
        }

        [Fact]
        public void ToggleDevice_InvalidIdDevice_ThrowsArgumentException()
        {
            // Arrange
            var service = CreateService();
            const int DEVICE_ID = 1;
            const bool TURN_ON = true;
            Device? fakeDevice = null;

            _deviceRepo.Setup(g => g.GetById(It.IsAny<int>())).Returns(fakeDevice);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => service.ToggleDevice(DEVICE_ID, TURN_ON));
        }

        [Fact]
        public void ToggleDevice_ValidIdDevice_CallsRepositoryUpdateOnce()
        {
            // Arrange
            var service = CreateService();
            const int DEVICE_ID = 1;
            const bool TURN_ON = true;
            var fakeDevice = new Device();

            _deviceRepo.Setup(g => g.GetById(It.IsAny<int>())).Returns(fakeDevice);

            // Act
            var result = service.ToggleDevice(DEVICE_ID, TURN_ON);

            // Assert
            _deviceRepo.Verify(r => r.Update(It.IsAny<Device>()), Times.Once);
        }

        [Fact]
        public void ToggleDevice_InvalidIdDevice_CallsRepositoryUpdateNever()
        {
            // Arrange
            var service = CreateService();
            const int DEVICE_ID = 1;
            const bool TURN_ON = true;
            Device? fakeDevice = null;

            _deviceRepo.Setup(g => g.GetById(It.IsAny<int>())).Returns(fakeDevice);

            // Act
            Record.Exception(() => service.ToggleDevice(DEVICE_ID, TURN_ON));

            // Assert
            _deviceRepo.Verify(r => r.Update(It.IsAny<Device>()), Times.Never);
        }

        [Fact]
        public void ToggleDevice_ValidDevice_UpdatesCorrectDeviceState()
        {
            // Arrange
            var service = CreateService();
            const int DEVICE_ID = 9;
            const bool TURN_ON = true;
            var device = new Device { Id = DEVICE_ID, Name = "LED Lamp", PowerUsageWatts = 20, IsOn = false };

            _deviceRepo.Setup(r => r.GetById(DEVICE_ID)).Returns(device);

            // Act
            service.ToggleDevice(DEVICE_ID, TURN_ON);

            // Assert
            _deviceRepo.Verify(r => r.Update(It.Is<Device>(d => d.Id == DEVICE_ID && d.IsOn)), Times.Once);
        }

        #endregion

        #region GetActiveDevicesMethod

        [Fact]
        public void GetActiveDevices_ExistingDevicesCollection_ReturnsExpectedCountOfElements()
        {
            // Arrange
            var service = CreateService();
            const int EXPECTED_DEVICE_COUNT = 2;
            var fakeDevicesList = new List<Device>
            {
                new Device() { IsOn = true },
                new Device() { IsOn = false },
                new Device() { IsOn = true }
            };

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);

            // Act
            var devicesList = service.GetActiveDevices();
            var actualCount = devicesList.ToList().Count;

            // Assert
            Assert.True(EXPECTED_DEVICE_COUNT == actualCount);
        }

        [Fact]
        public void GetActiveDevices_OneActiveDevice_ReturnsCountNotGreaterThanOne()
        {
            // Arrange
            var service = CreateService();
            const int MAXIMUM_DEVICE_COUNT = 1;
            var fakeDevicesList = new List<Device>
            {
                new Device { IsOn = false },
                new Device { IsOn = false },
                new Device { IsOn = true }
            };

            _deviceRepo.Setup(r => r.GetAll()).Returns(fakeDevicesList);

            // Act
            var result = service.GetActiveDevices();
            var actualCount = result.Count();

            // Assert
            Assert.False(actualCount > MAXIMUM_DEVICE_COUNT);
        }

        [Fact]
        public void GetActiveDevices_CollectionWithActiveDevices_ReturnsExpectedCollection()
        {
            // Arrange
            var service = CreateService();
            var expectedDevice = new Device() { IsOn = true };

            var fakeDevicesList = new List<Device>
            {
                expectedDevice,
                new Device { IsOn = false }
            };

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);

            // Act
            var result = service.GetActiveDevices();

            // Assert
            Assert.Contains(expectedDevice, result);
        }

        [Fact]
        public void GetActiveDevices_EmptyDevicesCollection_ReturnsEmptyCollection()
        {
            // Arrange
            var service = CreateService();
            var fakeDevicesList = new List<Device>();

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);

            // Act
            var result = service.GetActiveDevices();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetActiveDevices_ActiveDeviceExists_ReturnsNotEmptyCollection()
        {
            // Arrange
            var service = CreateService();
            var fakeDevicesList = new List<Device>()
            {
                new Device() {IsOn = true }
            };

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);

            // Act
            var result = service.GetActiveDevices();

            // Assert
            Assert.NotEmpty(result);
        }

        [Fact]
        public void GetActiveDevices_CallsRepositoryGetAllOnce()
        {
            // Arrange
            var service = CreateService();
            var fakeDevicesList = new List<Device>();

            _deviceRepo.Setup(g => g.GetAll()).Returns(fakeDevicesList);

            // Act
            service.GetActiveDevices();

            // Assert
            _deviceRepo.Verify(r => r.GetAll(), Times.Once);
        }

        #endregion
    }
}
