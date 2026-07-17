using System.Linq;
using NUnit.Framework;
using NataneToon.Editor;

namespace NataneToon.Tests.Editor
{
    public class NataneToonInspectorSectionRegistryTests
    {
        [Test]
        public void SectionKeys_AreUnique()
        {
            string[] duplicateKeys = NataneToonInspectorSectionRegistry.All
                .GroupBy(section => section.Key)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.That(duplicateKeys, Is.Empty);
        }

        [Test]
        public void ToggleSections_HaveToggleProperties()
        {
            string[] invalidSections = NataneToonInspectorSectionRegistry.All
                .Where(section => !string.IsNullOrEmpty(section.ToggleKeyword))
                .Where(section => string.IsNullOrEmpty(section.ToggleProperty))
                .Select(section => section.Key)
                .ToArray();

            Assert.That(invalidSections, Is.Empty);
        }

        [Test]
        public void ToggleKeywords_AreUnique()
        {
            string[] duplicateKeywords = NataneToonInspectorSectionRegistry.All
                .Where(section => !string.IsNullOrEmpty(section.ToggleKeyword))
                .GroupBy(section => section.ToggleKeyword)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.That(duplicateKeywords, Is.Empty);
        }

        [TestCase("mirror", "MirrorTexture")]
        [TestCase("顔", "FaceOrtho")]
        [TestCase("stencil", "Rendering")]
        [TestCase("sampler", "Performance")]
        public void Search_FindsExpectedSection(string query, string expectedKey)
        {
            string[] resultKeys = NataneToonInspectorSectionRegistry.Search(query)
                .Select(section => section.Key)
                .ToArray();

            Assert.That(resultKeys, Does.Contain(expectedKey));
        }

        [Test]
        public void EveryTab_HasSections()
        {
            foreach (NataneInspectorTab tab in System.Enum.GetValues(typeof(NataneInspectorTab)))
                Assert.That(NataneToonInspectorSectionRegistry.ForTab(tab), Is.Not.Empty, tab.ToString());
        }
    }
}
