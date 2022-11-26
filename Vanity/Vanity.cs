﻿using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using UnityEngine;

namespace Vanity;
[BepInPlugin(GUID, NAME, VERSION)]
public class Vanity : BaseUnityPlugin
{
  public const string GUID = "vanity";
  public const string NAME = "Vanity";
  public const string VERSION = "1.0";
#nullable disable
  public static ManualLogSource Log;
  public static CustomSyncedValue<string> VanityValue;
#nullable enable

  public static ConfigSync ConfigSync = new(GUID)
  {
    DisplayName = NAME,
    CurrentVersion = VERSION,
    IsLocked = true,
  };
  public void Awake()
  {
    Log = Logger;
    VanityValue = new CustomSyncedValue<string>(ConfigSync, "vanity_data");
    VanityValue.ValueChanged += () => VanityData.FromValue(VanityValue.Value);
    new Harmony(GUID).PatchAll();
    VanityData.CreateFile();
    VanityData.SetupWatcher();
    VanityData.FromFile();
  }
  public void Start()
  {
    CommandWrapper.Init();
  }


  private float timer = 0f;
  public void LateUpdate()
  {
    if (ZNet.instance && ZNet.instance.IsServer())
    {
      timer -= Time.deltaTime;
      if (timer <= 0f)
      {
        timer = 10f;
        VanityData.UpdatePlayerIds();
      }
    }
  }
}

[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
public class SetCommands
{
  static void Postfix()
  {
    ChangeEquipment.AddChangeEquipment();
    CommandWrapper.Register("playerid", (int index) =>
    {
      if (index == 0) return Helper.Players();
      return null;
    });
    new Terminal.ConsoleCommand("playerid", "[player id] - Copies the player id to the clipboard.", (args) =>
    {
      long id = 0;
      if (args.Length > 1)
      {
        id = Helper.GetPlayerID(string.Join(" ", args.Args.Skip(1)));
      }
      else
        id = Helper.GetPlayerID();
      args.Context.AddString(id.ToString());
      GUIUtility.systemCopyBuffer = id.ToString();
    }, optionsFetcher: () => Helper.Players());
  }
}
