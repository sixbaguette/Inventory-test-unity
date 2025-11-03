using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LootProfile", menuName = "Loot/Loot Profile", order = 0)]
public class LootProfile : ScriptableObject
{
    [Header("Règles globales")]
    [Tooltip("Plafond d’objets à générer au total (toutes catégories confondues). -1 = illimité.")]
    public int maxTotalItems = -1;

    [Tooltip("Tente de remplir l’inventaire au mieux (sans dépasser).")]
    public bool respectInventorySpace = true;

    [Tooltip("Seed fixe pour un résultat déterministe (0 = désactivé).")]
    public int fixedSeed = 0;

    [Header("Catégories")]
    public List<LootCategory> categories = new();
}

[Serializable]
public class LootCategory
{
    [Tooltip("Nom logique (Arme primaire, Munitions, Soins…)")]
    public string name = "Category";

    [Tooltip("Nombre min de tirages dans cette catégorie.")]
    public int minRolls = 0;

    [Tooltip("Nombre max de tirages dans cette catégorie.")]
    public int maxRolls = 1;

    [Tooltip("Entrées pondérées de la catégorie.")]
    public List<LootEntry> entries = new();
}

[Serializable]
public class LootEntry
{
    [Header("Références")]
    [Tooltip("ItemData (utilisé pour l’UI + logique).")]
    public ItemData itemData;

    [Tooltip("Prefab 3D optionnel si tu veux aussi du spawn monde un jour.")]
    public GameObject worldItemPrefab;

    [Header("Poids / Chances")]
    [Tooltip("Poids relatif (ex: 80 = commun, 5 = rare). Plus le poids est élevé, plus la chance est grande.")]
    public int weight = 10;

    [Header("Stack (si empilable)")]
    [Tooltip("Stack min si l’item est stackable.")]
    public int minStack = 1;

    [Tooltip("Stack max si l’item est stackable.")]
    public int maxStack = 30;
}
