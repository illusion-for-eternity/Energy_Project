using Energy_Project.Services.Interfaces;
using Moq;
using Energy_Project.Models;
using Energy_Project.Services;

namespace SmartHomeTests
{
    /// <summary>
    /// Unit tests for DeviceService.
    /// Tests device state changes, active device filtering,
    /// ensures repository methods are called correctly,
    /// and validates exception handling when a device is not found.
    /// </summary>
    public class DeviceServiceTests
    {
        private readonly Mock<IDeviceRepository> _deviceRepo = new();

        /// <summary>
        /// Helper method to create a DeviceService with a mocked repository.
        /// Reduces repeated setup code in each test.
        /// </summary>
        /// <returns>Instance of DeviceService using the mocked repository</returns>
        private DeviceService CreateService()
        {
            return new DeviceService(_deviceRepo.Object);
        }

        #region ToggleDeviceMethod

        /// <summary>
        /// Parameterized test for ToggleDevice method.
        /// Each InlineData provides a device ID, a requested state, and the expected result.
        /// </summary>
        /// <param name="deviceId">ID of the device to toggle</param>
        /// <param name="deviceTurnOnSetting">Requested device state</param>
        /// <param name="expected">Expected boolean result</param>
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

        /// <summary>
        /// Test: Turning on a device with default properties should return true.
        /// </summary>
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

        /// <summary>
        /// Test: Turning on an inactive device should return a state different from its initial state.
        /// </summary>
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

        /// <summary>
        /// Test: Toggling a non-existing device should throw ArgumentException.
        /// </summary>
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

        /// <summary>
        /// Test: Toggling an existing device should call repository.Update() exactly once.
        /// </summary>
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

        /// <summary>
        /// Test: Toggling a non-existing device should never call repository.Update().
        /// </summary>
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

        /// <summary>
        /// Test: Toggling a valid device should update the correct device state in the repository.
        /// </summary>
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

        /// <summary>
        /// Test: Retrieving active devices should return the expected number of devices.
        /// </summary>
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

        /// <summary>
        /// Test: Retrieving active devices should return a collection containing the expected device.
        /// </summary>
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

        /// <summary>
        /// Test: Retrieving active devices from an empty repository should return an empty collection.
        /// </summary>
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

        /// <summary>
        /// Test: Retrieving active devices should return a non-empty collection when an active device exists.
        /// </summary>
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

        /// <summary>
        /// Test: Retrieving active devices should call repository.GetAll() exactly once.
        /// </summary>
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
