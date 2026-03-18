using NUnit.Framework;
using UnityEngine;

namespace NataneToon.Editor.Tests
{
    [TestFixture]
    public class NataneToonSamplerBudgetEstimatorTests
    {
        private Material material;

        [SetUp]
        public void SetUp()
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("UI/Default");
            Assert.That(shader, Is.Not.Null, "Test shader could not be found.");
            material = new Material(shader);
        }

        [TearDown]
        public void TearDown()
        {
            if (material != null)
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void Estimate_WithDangerousLightingCombo_HitsLimitWithReserve()
        {
            EnableKeywords(
                "_2ND_TEXTURE",
                "_3RD_TEXTURE",
                "_COLOR_QUANTIZE",
                "_HAIR_SPECULAR",
                "_HATCHING",
                "_IRIDESCENCE",
                "_LTCGI",
                "_NORMALMAP",
                "_RIM_LIGHT",
                "_SCREEN_TONE",
                "_USE_LIGHT_VOLUME");

            var estimate = NataneToonSamplerBudgetEstimator.Estimate(material);

            Assert.That(estimate.IsAtLimit, Is.True);
            Assert.That(estimate.EstimatedSamplers, Is.EqualTo(NataneToonSamplerBudgetEstimator.SamplerLimit));
            Assert.That(estimate.HasLightVolumeLtcgiCombo, Is.True);
            Assert.That(estimate.HasCriticalLightingCombo, Is.True);
        }

        [Test]
        public void EvaluateEnable_WithNearLimitMaterial_BlocksExtraHeavyFeature()
        {
            EnableKeywords(
                "_2ND_TEXTURE",
                "_3RD_TEXTURE",
                "_HAIR_SPECULAR",
                "_EMISSION",
                "_LTCGI",
                "_MATCAP",
                "_NORMALMAP",
                "_REFLECTION",
                "_RIM_LIGHT",
                "_SCREEN_TONE",
                "_USE_LIGHT_VOLUME");

            var current = NataneToonSamplerBudgetEstimator.Estimate(material);
            var evaluation = NataneToonSamplerBudgetEstimator.EvaluateEnable(material, "_HATCHING");

            Assert.That(current.IsNearLimit, Is.True);
            Assert.That(evaluation.CanEnable, Is.False);
            Assert.That(evaluation.AddedSamplers, Is.EqualTo(2));
            Assert.That(evaluation.AfterEnable.IsOverLimit, Is.True);
        }

        [Test]
        public void Estimate_WithOnlyLightVolumeAndLtcgi_SetsComboFlagWithoutOverflow()
        {
            EnableKeywords("_USE_LIGHT_VOLUME", "_LTCGI");

            var estimate = NataneToonSamplerBudgetEstimator.Estimate(material);

            Assert.That(estimate.HasLightVolumeLtcgiCombo, Is.True);
            Assert.That(estimate.HasCriticalLightingCombo, Is.False);
            Assert.That(estimate.HasScreenSpaceLightingCombo, Is.False);
            Assert.That(estimate.IsOverLimit, Is.False);
            Assert.That(estimate.EstimatedSamplers, Is.EqualTo(NataneToonSamplerBudgetEstimator.BaseSamplerCount + 5));
        }

        [Test]
        public void EvaluateEnable_WithSharedMaskFeature_DoesNotAddSamplerCost()
        {
            EnableKeywords("_USE_LIGHT_VOLUME", "_LTCGI");

            var evaluation = NataneToonSamplerBudgetEstimator.EvaluateEnable(material, "_RIM_LIGHT");

            Assert.That(evaluation.CanEnable, Is.True);
            Assert.That(evaluation.AddedSamplers, Is.EqualTo(0));
            Assert.That(evaluation.AfterEnable.EstimatedSamplers, Is.EqualTo(evaluation.Current.EstimatedSamplers));
        }

        [Test]
        public void EvaluateEnable_WithSharedBlurMaskFeature_DoesNotAddSamplerCost()
        {
            EnableKeywords("_USE_LIGHT_VOLUME", "_LTCGI");

            var screenTone = NataneToonSamplerBudgetEstimator.EvaluateEnable(material, "_SCREEN_TONE");
            var ao = NataneToonSamplerBudgetEstimator.EvaluateEnable(material, "_USE_AO");

            Assert.That(screenTone.CanEnable, Is.True);
            Assert.That(screenTone.AddedSamplers, Is.EqualTo(0));
            Assert.That(screenTone.AfterEnable.EstimatedSamplers, Is.EqualTo(screenTone.Current.EstimatedSamplers));

            Assert.That(ao.CanEnable, Is.True);
            Assert.That(ao.AddedSamplers, Is.EqualTo(0));
            Assert.That(ao.AfterEnable.EstimatedSamplers, Is.EqualTo(ao.Current.EstimatedSamplers));
        }

        [Test]
        public void Estimate_WithScreenEdgeLightingCombo_HitsLimit()
        {
            EnableKeywords(
                "_2ND_TEXTURE",
                "_3RD_TEXTURE",
                "_4TH_TEXTURE",
                "_LTCGI",
                "_PARALLAX",
                "_PROCEDURAL_MATCAP",
                "_RIM_LIGHT",
                "_SCREEN_EDGE",
                "_SSS",
                "_USE_AO",
                "_USE_LIGHT_VOLUME");

            var estimate = NataneToonSamplerBudgetEstimator.Estimate(material);

            Assert.That(estimate.HasLightVolumeLtcgiCombo, Is.True);
            Assert.That(estimate.HasCriticalLightingCombo, Is.False);
            Assert.That(estimate.HasScreenSpaceLightingCombo, Is.True);
            Assert.That(estimate.IsAtLimit, Is.True);
        }

        private void EnableKeywords(params string[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                material.EnableKeyword(keywords[i]);
            }
        }
    }
}
