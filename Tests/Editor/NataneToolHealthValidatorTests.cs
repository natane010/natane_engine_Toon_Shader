using System.Linq;
using NUnit.Framework;

namespace NataneToon.Editor.Tests
{
    [TestFixture]
    public class NataneToolHealthValidatorTests
    {
        private bool originalIsJapanese;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            originalIsJapanese = NataneToonLocalization.IsJapanese;
        }

        [TearDown]
        public void TearDown()
        {
            SetJapanese(originalIsJapanese);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            SetJapanese(originalIsJapanese);
        }

        [Test]
        public void ValidateTool_WithRegisteredMaterialValidatorMenuPath_ReturnsNull()
        {
            var result = NataneToolHealthValidator.ValidateTool(NataneToolMenuPaths.MaterialValidator);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ValidateTool_WithUnknownMenuPath_ReturnsLocalizedWarning()
        {
            const string unknownMenuPath = "Tools/Natane/Unknown Tool";

            SetJapanese(true);
            var japaneseResult = NataneToolHealthValidator.ValidateTool(unknownMenuPath);

            Assert.That(japaneseResult, Is.Not.Null);
            Assert.That(japaneseResult.severity, Is.EqualTo(NataneToolHealthValidator.DiagnosticSeverity.Warning));
            StringAssert.Contains("登録", japaneseResult.message);
            StringAssert.Contains("エントリ", japaneseResult.suggestion);

            SetJapanese(false);
            var englishResult = NataneToolHealthValidator.ValidateTool(unknownMenuPath);

            Assert.That(englishResult, Is.Not.Null);
            Assert.That(englishResult.severity, Is.EqualTo(NataneToolHealthValidator.DiagnosticSeverity.Warning));
            StringAssert.Contains("not registered", englishResult.message);
            StringAssert.Contains("Add an entry", englishResult.suggestion);
        }

        [Test]
        public void RunFullDiagnostics_LocalizesHealthyResultMessage()
        {
            SetJapanese(true);
            var japaneseResult = NataneToolHealthValidator.RunFullDiagnostics()
                .FirstOrDefault(result => result.toolKey == "MaterialValidator");

            Assert.That(japaneseResult, Is.Not.Null);
            Assert.That(japaneseResult.severity, Is.EqualTo(NataneToolHealthValidator.DiagnosticSeverity.OK));
            Assert.That(japaneseResult.message, Is.EqualTo("正常"));

            SetJapanese(false);
            var englishResult = NataneToolHealthValidator.RunFullDiagnostics()
                .FirstOrDefault(result => result.toolKey == "MaterialValidator");

            Assert.That(englishResult, Is.Not.Null);
            Assert.That(englishResult.message, Is.EqualTo("Healthy"));
        }

        [Test]
        public void RunFullDiagnostics_IncludesDashboardEntry()
        {
            var results = NataneToolHealthValidator.RunFullDiagnostics();

            Assert.That(results.Any(result => result.toolKey == "Dashboard"), Is.True);
        }

        [Test]
        public void GetOverallHealth_ForCurrentProject_IsHealthy()
        {
            Assert.That(
                NataneToolHealthValidator.GetOverallHealth(),
                Is.EqualTo(NataneToolHealthValidator.HealthStatus.Healthy));
        }

        private static void SetJapanese(bool japanese)
        {
            if (NataneToonLocalization.IsJapanese != japanese)
            {
                NataneToonLocalization.ToggleLanguage();
            }
        }
    }
}
