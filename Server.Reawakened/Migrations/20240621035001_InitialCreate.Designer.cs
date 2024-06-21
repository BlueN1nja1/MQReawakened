﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Server.Reawakened.Database;

#nullable disable

namespace Server.Reawakened.Migrations
{
    [DbContext(typeof(ReawakenedDatabase))]
    [Migration("20240621035001_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.6");

            modelBuilder.Entity("Server.Reawakened.Database.Characters.CharacterDbEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AbilityPower")
                        .HasColumnType("INTEGER");

                    b.Property<string>("AchievementObjectives")
                        .HasColumnType("TEXT");

                    b.Property<int>("ActiveQuestId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Allegiance")
                        .HasColumnType("INTEGER");

                    b.Property<int>("BadgePoints")
                        .HasColumnType("INTEGER");

                    b.Property<string>("BestMinigameTimes")
                        .HasColumnType("TEXT");

                    b.Property<string>("Blocked")
                        .HasColumnType("TEXT");

                    b.Property<int>("Cash")
                        .HasColumnType("INTEGER");

                    b.Property<string>("CharacterName")
                        .HasColumnType("TEXT");

                    b.Property<string>("CollectedIdols")
                        .HasColumnType("TEXT");

                    b.Property<string>("Colors")
                        .HasColumnType("TEXT");

                    b.Property<string>("CompletedQuests")
                        .HasColumnType("TEXT");

                    b.Property<string>("CurrentCollectedDailies")
                        .HasColumnType("TEXT");

                    b.Property<int>("CurrentLife")
                        .HasColumnType("INTEGER");

                    b.Property<string>("CurrentQuestDailies")
                        .HasColumnType("TEXT");

                    b.Property<string>("DiscoveredStats")
                        .HasColumnType("TEXT");

                    b.Property<string>("EmailMessages")
                        .HasColumnType("TEXT");

                    b.Property<string>("Emails")
                        .HasColumnType("TEXT");

                    b.Property<string>("EquippedBinding")
                        .HasColumnType("TEXT");

                    b.Property<string>("EquippedItems")
                        .HasColumnType("TEXT");

                    b.Property<string>("Events")
                        .HasColumnType("TEXT");

                    b.Property<int>("ExternalDamageResistPointsFire")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExternalDamageResistPointsIce")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExternalDamageResistPointsLightning")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExternalDamageResistPointsPoison")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExternalDamageResistPointsStandard")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExternalStatusEffectResistSecondsFreeze")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExternalStatusEffectResistSecondsSlow")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExternalStatusEffectResistSecondsStun")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ForceTribeSelection")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Friends")
                        .HasColumnType("TEXT");

                    b.Property<int>("Gender")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GlobalLevel")
                        .HasColumnType("INTEGER");

                    b.Property<long>("GuestPassExpiry")
                        .HasColumnType("INTEGER");

                    b.Property<string>("HotbarButtons")
                        .HasColumnType("TEXT");

                    b.Property<int>("InteractionStatus")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InternalDamageResistPointsFire")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InternalDamageResistPointsIce")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InternalDamageResistPointsLightning")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InternalDamageResistPointsPoison")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InternalDamageResistPointsStandard")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Items")
                        .HasColumnType("TEXT");

                    b.Property<int>("LevelId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MaxLife")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Muted")
                        .HasColumnType("TEXT");

                    b.Property<int>("NCash")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("PetAutonomous")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PetItemId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Pets")
                        .HasColumnType("TEXT");

                    b.Property<string>("Properties")
                        .HasColumnType("TEXT");

                    b.Property<string>("QuestLog")
                        .HasColumnType("TEXT");

                    b.Property<string>("RecipeList")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Registered")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Reputation")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ReputationForCurrentLevel")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ReputationForNextLevel")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShouldExpireGuestPass")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("SpawnOnBackPlane")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SpawnPointId")
                        .HasColumnType("TEXT");

                    b.Property<float>("SpawnPositionX")
                        .HasColumnType("REAL");

                    b.Property<float>("SpawnPositionY")
                        .HasColumnType("REAL");

                    b.Property<string>("TribesDiscovered")
                        .HasColumnType("TEXT");

                    b.Property<string>("TribesProgression")
                        .HasColumnType("TEXT");

                    b.Property<int>("UserUuid")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Characters");
                });

            modelBuilder.Entity("Server.Reawakened.Database.Users.UserInfoDbEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AuthToken")
                        .HasColumnType("TEXT");

                    b.Property<string>("CharacterIds")
                        .HasColumnType("TEXT");

                    b.Property<int>("ChatLevel")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("DateOfBirth")
                        .HasColumnType("TEXT");

                    b.Property<int>("Gender")
                        .HasColumnType("INTEGER");

                    b.Property<string>("LastCharacterSelected")
                        .HasColumnType("TEXT");

                    b.Property<string>("Mail")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Member")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Region")
                        .HasColumnType("TEXT");

                    b.Property<string>("SignUpExperience")
                        .HasColumnType("TEXT");

                    b.Property<string>("TrackingShortId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("UserInfos");
                });
#pragma warning restore 612, 618
        }
    }
}
