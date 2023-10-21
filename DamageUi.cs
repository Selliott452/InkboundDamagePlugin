// using System.Collections.Generic;
// using System.Linq;
// using TMPro;
// using UnityEngine;
//
// namespace InkboundDamage;
//
// public class DamageUI : MonoBehaviour
// {
//     public GameTracker Game;
//     
//     private Dictionary<int, PlayerDamageUI> PlayerUIs = new();
//
//     public void Update()
//     {
//         if (Game != null)
//         {
//             foreach (var player in Game.Players)
//             {
//                 if (!PlayerUIs.Keys.ToList().Contains(player.ID))
//                 {
//                     var playerui = gameObject.AddComponent<PlayerDamageUI>();
//                     playerui.player = player;
//
//                     PlayerUIs.Add(player.ID, playerui);
//                 }
//             }
//
//             foreach (var ui in PlayerUIs.Values)
//             {
//                 ui.sync();
//             }
//         }
//     }
//
//
// }
//
// public class PlayerDamageUI : MonoBehaviour
// {
//     public Player player;
//
//     public Dictionary<string, PlayerDamageLabel> labels = new ();
//     public void sync()
//     {
//         
//         foreach (var damageDealtKey in player.DamageDealt.Keys)
//         {
//             if (!labels.Keys.ToList().Contains(damageDealtKey))
//             {
//                 var newLabel = gameObject.AddComponent<PlayerDamageLabel>();
//                 newLabel.UpdateLabel(damageDealtKey);
//                 labels.Add(damageDealtKey, newLabel);
//             }
//         }
//     }
// }
//
// public class PlayerDamageLabel : MonoBehaviour
// {
//     public TextMeshProUGUI label;
//
//     private void Awake()
//     {
//         label = gameObject.AddComponent<TextMeshProUGUI>();
//     }
//
//     public void UpdateLabel(string txt)
//     {
//         label.text = txt;
//     }
// }