﻿using OWML.Utils;
using QSB;
using QSB.Animation.Player;
using QSB.Player;
using QSB.Utility;
using QSB.WorldSync;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkinChangerQSB;

/// <summary>
/// This was largely copied from https://github.com/xen-42/ow-qsb-skins but changing the method of applying skins to use SkinChanger
/// </summary>
public class SkinChangerQSB : MonoBehaviour
{
    public static SkinChangerQSB Instance { get; private set; }

    public string LocalSkin { get; private set; }

    // Links QSB playerID to current skin name and the game object they have on representing it
    private readonly Dictionary<uint, (string skinName, GameObject currentMesh)> _skins = new();

    public static string ChangeSkinMessage => nameof(ChangeSkinMessage);

    public void Start()
    {
        SkinChanger.SkinChanger.instance.skinChanged += OnSkinChanged;

        Instance = this;

        LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        QSBPlayerManager.OnAddPlayer += OnPlayerAdded;

        QSBHelper.API.RegisterHandler<string>(ChangeSkinMessage, OnReceiveChangeSkinMessage);
        QSBCore.RegisterNotRequiredForAllPlayers(SkinChanger.SkinChanger.instance);
    }

    private void OnCompleteSceneLoad(OWScene originalScene, OWScene loadScene)
    {
        if (loadScene == OWScene.SolarSystem || loadScene == OWScene.EyeOfTheUniverse)
        {
            // Wait for QSB to finish connecting and syncing first
            Delay.RunWhen(
                () => QSBWorldSync.AllObjectsReady,
                () => ChangePlayerSkin(QSBPlayerManager.LocalPlayer, LocalSkin)
            );
        }
    }

    public void OnSkinChanged(string skin)
    {
        LocalSkin = skin;
        if (QSBWorldSync.AllObjectsReady)
        {
            ChangePlayerSkin(QSBPlayerManager.LocalPlayer, skin);
        }
    }

    private void OnPlayerAdded(PlayerInfo player)
    {
        // Send them info about our skin
        // Make sure they've finished loading in first
        Delay.RunWhen(
            () => player.Body != null,
            () => SendChangeSkinMessage(LocalSkin, to: player.PlayerId)
        );
    }

    public void OnReceiveChangeSkinMessage(uint From, string Data)
    {
        Delay.RunWhen(
           () => QSBPlayerManager.GetPlayer(From).Body != null,
           () => Instance.ChangePlayerSkin(QSBPlayerManager.GetPlayer(From), Data)
        );
    }

    public static void SendChangeSkinMessage(string skin, uint to = uint.MaxValue)
    {
        QSBHelper.API.SendMessage(ChangeSkinMessage, skin, to: to);
    }

    public void ChangePlayerSkin(PlayerInfo player, string skinName)
    {
        DebugLogger.Write($"Changing skin on {player.PlayerId} to {skinName}");

        // Replace skin
        var isDefaultSkin = skinName == "Hatchling";

        if (player.IsLocalPlayer)
        {
            // Immediately tell all other clients to alter our skin
            SendChangeSkinMessage(skinName);

            // SkinChanger base will handle changing our skin for us, unless it's the hatchling where we have to insert our own jank
            SetHatchlingActive(player.Body, isDefaultSkin, false, PlayerState.IsWearingSuit());
        }
        else
        {
            if (_skins.TryGetValue(player.PlayerId, out var skin))
            {
                if (skin.skinName == skinName)
                {
                    // Already has that skin
                    return;
                }
                else if (skin.currentMesh != null)
                {
                    GameObject.Destroy(skin.currentMesh);
                }
            }

            SetHatchlingActive(player.Body, isDefaultSkin, true, player.SuitedUp);

            if (isDefaultSkin)
            {
                _skins[player.PlayerId] = (skinName, null);
                player.Body.GetComponentInChildren<AnimatorMirror>()
                    .SetValue("_to", player.Body.transform.Find("REMOTE_Traveller_HEA_Player_v2").GetComponent<Animator>());
                player.Body.GetComponentInChildren<HelmetAnimator>().enabled = true;
            }
            else
            {
                DebugLogger.Write("Creating new skin object");

                if (skinName == "Inhabitant")
                {
                    var prefabI = SkinChanger.SkinChanger.instance.characters.First(x => x.SettingName == "N0").GameObject;
                    var meshI = prefabI.InstantiateInactive();
                    meshI.transform.parent = player.Body.transform;
                    meshI.transform.localPosition = new Vector3(0, -1.03f, -0.2f);
                    meshI.transform.localScale = Vector3.one * .1f;
                    meshI.transform.localRotation = Quaternion.identity;
                    Component.DestroyImmediate(meshI.GetComponent<PlayerAnimController>());
                    player.Body.GetComponentInChildren<AnimatorMirror>()
                        .SetValue("_to", meshI.GetComponent<Animator>());
                    meshI.SetActive(true);

                    // Doesn't work on custom meshes and can cause a floating helmet to appear
                    player.Body.GetComponentInChildren<HelmetAnimator>().enabled = false;

                    _skins[player.PlayerId] = ("N0", meshI);
                }
                else if (skinName == "Nomai")
                {
                    var prefabS = SkinChanger.SkinChanger.instance.characters.First(x => x.SettingName == "N1").GameObject;
                    var meshS = prefabS.InstantiateInactive();
                    meshS.transform.parent = player.Body.transform;
                    meshS.transform.localPosition = new Vector3(0, -1.03f, -0.2f);
                    meshS.transform.localScale = Vector3.one * .1f;
                    meshS.transform.localRotation = Quaternion.identity;
                    Component.DestroyImmediate(meshS.GetComponent<PlayerAnimController>());
                    player.Body.GetComponentInChildren<AnimatorMirror>()
                        .SetValue("_to", meshS.GetComponent<Animator>());
                    meshS.SetActive(true);

                    // Doesn't work on custom meshes and can cause a floating helmet to appear
                    player.Body.GetComponentInChildren<HelmetAnimator>().enabled = false;

                    _skins[player.PlayerId] = ("N1", meshS);
                }

                else
                {
                    var prefab = SkinChanger.SkinChanger.instance.characters.First(x => x.SettingName == skinName).GameObject;
                    var mesh = prefab.InstantiateInactive();
                    mesh.transform.parent = player.Body.transform;
                    mesh.transform.localPosition = new Vector3(0, -1.03f, -0.2f);
                    mesh.transform.localScale = Vector3.one * .1f;
                    mesh.transform.localRotation = Quaternion.identity;
                    Component.DestroyImmediate(mesh.GetComponent<PlayerAnimController>());
                    player.Body.GetComponentInChildren<AnimatorMirror>()
                        .SetValue("_to", mesh.GetComponent<Animator>());
                    mesh.SetActive(true);

                    // Doesn't work on custom meshes and can cause a floating helmet to appear
                    player.Body.GetComponentInChildren<HelmetAnimator>().enabled = false;

                    _skins[player.PlayerId] = (skinName, mesh);
                }
            }
        }
    }

    private Transform[] GetChildrenIncludingInactive(Transform parent)
    {
        List<Transform> children = new();
        for (int i = 0; i < parent.childCount; i++)
        {
            children.Add(parent.GetChild(i));
        }
        return children.ToArray();
    }

    private void SetHatchlingActive(GameObject playerBody, bool active, bool isRemote, bool isSuited)
    {
        var hatchlingBody = playerBody.transform.Find((isRemote ? "REMOTE_" : string.Empty) + "Traveller_HEA_Player_v2").gameObject;
        hatchlingBody.SetActive(true);

        if (active)
        {
            var playerMeshContainer = hatchlingBody.transform.Find("PlayerMeshContainer");
            if (playerMeshContainer == null)
            {
                playerMeshContainer = new GameObject("PlayerMeshContainer").transform;
                playerMeshContainer.transform.SetParent(hatchlingBody.transform, false);
                playerMeshContainer.gameObject.SetActive(false);
            }

            // Move all meshes from the inactive container to the regular object
            foreach (var child in GetChildrenIncludingInactive(playerMeshContainer))
            {
                child.SetParent(hatchlingBody.transform);
            }

            // Fix state of suit vs no suit
            hatchlingBody.transform.Find("player_mesh_noSuit:Traveller_HEA_Player").gameObject.SetActive(isSuited);
            hatchlingBody.transform.Find("Traveller_Mesh_v01:Traveller_Geo").gameObject.SetActive(isSuited);
        }
        else
        {
            var playerMeshContainer = hatchlingBody.transform.Find("PlayerMeshContainer");
            if (playerMeshContainer == null)
            {
                playerMeshContainer = new GameObject("PlayerMeshContainer").transform;
                playerMeshContainer.transform.SetParent(hatchlingBody.transform, false);
                playerMeshContainer.gameObject.SetActive(false);
            }
            // Move all meshes from the inactive container to the regular object
            foreach (var child in GetChildrenIncludingInactive(hatchlingBody.transform))
            {
                if (child == playerMeshContainer) continue;
                child.transform.SetParent(playerMeshContainer);
            }
        }
    }
}