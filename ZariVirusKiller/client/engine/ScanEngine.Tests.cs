using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ZariVirusKiller.Engine.Tests
{
    [TestClass]
    public class ScanEngineTests
    {
        private ScanEngine _scanEngine;
        private readonly string _testServerUrl = "http://localhost:5000";
        
        [TestInitialize]
        public void Initialize()
        {
            _scanEngine = new ScanEngine(_testServerUrl);
        }
        
        [TestMethod]
        public async Task ScanFile_WithNonExistentFile_ReturnsErrorResult()
        {
            // Arrange
            string nonExistentFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
            // Act
            var result = await _scanEngine.ScanFileAsync(nonExistentFilePath);
            
            // Assert
            Assert.IsFalse(result.IsInfected);
            Assert.AreEqual("File not found", result.ErrorMessage);
        }
        
        [TestMethod]
        public async Task ScanFile_WithCleanFile_ReturnsCleanResult()
        {
            // Arrange
            string tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "This is a clean test file.");
            
            try
            {
                // Initialize the engine with test definitions
                await _scanEngine.InitializeAsync();
                
                // Act
                var result = await _scanEngine.ScanFileAsync(tempFilePath);
                
                // Assert
                Assert.IsFalse(result.IsInfected);
                Assert.IsNull(result.ThreatName);
                Assert.IsNull(result.ErrorMessage);
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
        
        [TestMethod]
        public async Task UpdateDefinitions_ReturnsTrue()
        {
            // Act
            bool result = await _scanEngine.UpdateDefinitionsAsync();
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [TestMethod]
        public void QuarantineFile_WithNonExistentFile_ReturnsFalse()
        {
            // Arrange
            string nonExistentFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
            // Act
            bool result = _scanEngine.QuarantineFile(nonExistentFilePath);
            
            // Assert
            Assert.IsFalse(result);
        }
    }
}