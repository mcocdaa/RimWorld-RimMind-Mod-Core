using System.Collections.Generic;
using RimMind.Core.Internal;
using Xunit;

namespace RimMind.Core.Tests
{
    public class PawnDataTests
    {
        [Fact]
        public void PawnData_Defaults_AreCorrect()
        {
            var data = new PawnData();

            Assert.Null(data.Name);
            Assert.Equal(0, data.Age);
            Assert.Null(data.GenderLabel);
            Assert.Null(data.RaceLabel);
            Assert.Null(data.ChildhoodTitle);
            Assert.Null(data.AdulthoodTitle);
            Assert.Empty(data.TraitLabels);

            Assert.Equal(0f, data.MoodPercent);
            Assert.Null(data.MoodString);
            Assert.False(data.InMentalState);
            Assert.Null(data.MentalStateInspectLine);
            Assert.False(data.Downed);
            Assert.False(data.MentalBreakImminent);
            Assert.Empty(data.MoodThoughts);

            Assert.Empty(data.Hediffs);
            Assert.Empty(data.Capacities);
            Assert.Empty(data.Skills);

            Assert.Null(data.CurrentJobReport);
            Assert.Null(data.CurrentJobDefLabel);

            Assert.Empty(data.WorkPriorities);

            Assert.Null(data.WeaponLabel);
            Assert.Empty(data.ApparelLabels);
            Assert.Empty(data.InventoryItems);

            Assert.Null(data.RoomLabel);
            Assert.Equal(0f, data.Temperature);
            Assert.False(data.HasMap);

            Assert.Empty(data.Relations);

            Assert.False(data.InCombat);
            Assert.False(data.Drafted);
            Assert.Null(data.EnemyTargetLabel);
            Assert.Null(data.EnemyTargetHpPercent);

            Assert.Null(data.IdeologyName);
            Assert.Null(data.IdeologyMemes);

            Assert.Empty(data.NotableGenes);
            Assert.Empty(data.NearbyPawnNames);

            Assert.Equal(0, data.ColonistCount);
            Assert.Equal(0f, data.ColonyWealth);
            Assert.Equal(0, data.ThreatCount);

            Assert.Null(data.WeatherLabel);
            Assert.Null(data.TimeString);
            Assert.Equal(0, data.TimeHour);
            Assert.Equal(0, data.TimeDay);
            Assert.Null(data.SeasonLabel);
        }

        [Fact]
        public void PawnData_FieldAssignment_Works()
        {
            var data = new PawnData
            {
                Name = "Alice",
                Age = 25,
                GenderLabel = "Female",
                RaceLabel = "Baseliner",
                MoodPercent = 75.5f,
                MoodString = "Content",
                InMentalState = false,
                Downed = false,
                Temperature = 21.3f,
                HasMap = true,
                InCombat = true,
                Drafted = true,
                EnemyTargetLabel = "Bob",
                EnemyTargetHpPercent = 60f,
                ColonistCount = 5,
                ColonyWealth = 15000f,
                ThreatCount = 2
            };

            Assert.Equal("Alice", data.Name);
            Assert.Equal(25, data.Age);
            Assert.Equal("Female", data.GenderLabel);
            Assert.Equal("Baseliner", data.RaceLabel);
            Assert.Equal(75.5f, data.MoodPercent);
            Assert.Equal("Content", data.MoodString);
            Assert.False(data.InMentalState);
            Assert.False(data.Downed);
            Assert.Equal(21.3f, data.Temperature);
            Assert.True(data.HasMap);
            Assert.True(data.InCombat);
            Assert.True(data.Drafted);
            Assert.Equal("Bob", data.EnemyTargetLabel);
            Assert.Equal(60f, data.EnemyTargetHpPercent);
            Assert.Equal(5, data.ColonistCount);
            Assert.Equal(15000f, data.ColonyWealth);
            Assert.Equal(2, data.ThreatCount);
        }

        [Fact]
        public void PawnData_CollectionsAreMutable()
        {
            var data = new PawnData();

            data.TraitLabels.Add("Kind");
            data.TraitLabels.Add("Industrious");
            Assert.Equal(2, data.TraitLabels.Count);

            data.Hediffs.Add(new HediffRecord
            {
                PartLabel = "LeftArm",
                HediffLabel = "Scar",
                IsBad = true,
                Severity = 0.3f,
                Visible = true
            });
            Assert.Single(data.Hediffs);
            Assert.Equal("LeftArm", data.Hediffs[0].PartLabel);
            Assert.True(data.Hediffs[0].IsBad);

            data.Skills.Add(("Shooting", 8));
            data.Skills.Add(("Melee", 12));
            Assert.Equal(2, data.Skills.Count);
            Assert.Equal(12, data.Skills[1].Level);

            data.MoodThoughts.Add(("Ate bad food", -3f));
            Assert.Single(data.MoodThoughts);

            data.InventoryItems["Steel"] = 75;
            data.InventoryItems["Wood"] = 120;
            Assert.Equal(2, data.InventoryItems.Count);
            Assert.Equal(75, data.InventoryItems["Steel"]);

            data.Relations.Add(("Spouse", "Bob"));
            Assert.Single(data.Relations);

            data.WorkPriorities.Add((1, "Firefight"));
            data.WorkPriorities.Add((2, "Patient"));
            Assert.Equal(2, data.WorkPriorities.Count);
        }

        [Fact]
        public void HediffRecord_Defaults_AreCorrect()
        {
            var record = new HediffRecord();
            Assert.Null(record.PartLabel);
            Assert.Null(record.HediffLabel);
            Assert.False(record.IsBad);
            Assert.Equal(0f, record.Severity);
            Assert.False(record.Visible);
        }

        [Fact]
        public void HediffRecord_ValueSemantics()
        {
            var a = new HediffRecord
            {
                PartLabel = "Torso",
                HediffLabel = "Bruise",
                IsBad = true,
                Severity = 0.5f,
                Visible = true
            };
            var b = a;
            Assert.Equal(a.PartLabel, b.PartLabel);
            Assert.Equal(a.HediffLabel, b.HediffLabel);
            Assert.Equal(a.IsBad, b.IsBad);
            Assert.Equal(a.Severity, b.Severity);
            Assert.Equal(a.Visible, b.Visible);

            b.Severity = 0.8f;
            Assert.Equal(a.Severity, b.Severity);
        }

        [Fact]
        public void PawnData_NullableFields_Work()
        {
            var data = new PawnData();

            Assert.Null(data.EnemyTargetHpPercent);
            data.EnemyTargetHpPercent = 45.5f;
            Assert.Equal(45.5f, data.EnemyTargetHpPercent.Value);

            data.EnemyTargetHpPercent = null;
            Assert.Null(data.EnemyTargetHpPercent);
        }

        [Fact]
        public void PawnData_MultipleInstances_Independent()
        {
            var a = new PawnData { Name = "A", MoodPercent = 50f };
            var b = new PawnData { Name = "B", MoodPercent = 80f };

            a.TraitLabels.Add("Kind");
            b.TraitLabels.Add("Brave");

            Assert.Single(a.TraitLabels);
            Assert.Single(b.TraitLabels);
            Assert.Equal("Kind", a.TraitLabels[0]);
            Assert.Equal("Brave", b.TraitLabels[0]);
        }
    }
}
