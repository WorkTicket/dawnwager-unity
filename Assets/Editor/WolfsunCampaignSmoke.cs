using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Wolfsun.Editor
{
    internal static class WolfsunCampaignSmoke
    {
        [MenuItem("Dawnwager/Check Campaign in Play Mode")]
        private static void Run()
        {
            var opening = Find("Begin campaign") ?? Find("Continue campaign");
            Press(opening);
            Require(Find("Fimbulwinter") != null, "The campaign route did not render.");
            var enter = Find("Enter battle") ?? Find("Replay battle");
            Press(enter);
            AfterCardMotion(() =>
            {
                var panel = FindRect("Opening hand");
                Require(panel, "The opening hand did not render.");
                var first = panel.GetComponentsInChildren<Button>().FirstOrDefault(button =>
                    button.GetComponent<RawImage>() != null);
                Press(first);
                Press(Find("Replace 1"));
                AfterCardMotion(CheckBattle);
            });
        }

        private static void CheckBattle()
        {
            Require(FindRect("Opening hand") == null, "Opening hand did not dismiss.");

            Require(UnityEngine.Object.FindObjectsByType<RectTransform>().Count(
                rect => rect.gameObject.activeInHierarchy && rect.name == "Drop area") == 2, "The two invisible lane zones are missing.");
            var graphics = UnityEngine.Object.FindObjectsByType<RawImage>();
            var backdrop = graphics.SingleOrDefault(graphic => graphic.name == "Full viewport artwork");
            Require(backdrop && backdrop.rectTransform.anchorMin == Vector2.zero && backdrop.rectTransform.anchorMax == Vector2.one,
                "The environment artwork must cover the complete viewport.");
            Require(!UnityEngine.Object.FindObjectsByType<Text>().Any(label =>
                label.text == "Your hand" || label.text == "Your next move"), "The old hand tray or sidebar is visible.");
            var hand = UnityEngine.Object.FindObjectsByType<RectTransform>()
                .SingleOrDefault(rect => rect.name == "Fanned cards");
            Require(hand && hand.childCount > 0, "No playable hand was presented after the opening.");
            var card = hand.GetChild(0);
            var hover = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(card.gameObject, hover, ExecuteEvents.pointerEnterHandler);
            var preview = UnityEngine.Object.FindObjectsByType<RectTransform>()
                .SingleOrDefault(rect => rect.name == "Card preview");
            Require(preview && preview.Find("Rules panel"), "Hover did not produce a card face with rules.");
            Require(!preview.GetComponent<CanvasGroup>().blocksRaycasts, "The card preview intercepts pointer input.");
            ExecuteEvents.Execute(card.gameObject, hover, ExecuteEvents.pointerExitHandler);
            Require(!preview.gameObject.activeInHierarchy, "The preview stayed visible after pointer exit.");
            Require(UnityEngine.Object.FindObjectsByType<Text>().Count(label => label.text == "Health") >= 2,
                "One of the hero health labels is missing.");
            Debug.Log("Dawnwager campaign smoke passed: first battle, card preview, hero health, drop zones, and full viewport artwork.");
        }

        private static RectTransform FindRect(string name) => UnityEngine.Object.FindObjectsByType<RectTransform>()
            .FirstOrDefault(rect => rect.name == name);

        private static void AfterCardMotion(Action next)
        {
            double deadline = EditorApplication.timeSinceStartup + 6;
            void Wait()
            {
                if (EditorApplication.isPlaying && FindRect("Card animation") && EditorApplication.timeSinceStartup < deadline) return;
                EditorApplication.update -= Wait;
                Require(EditorApplication.isPlaying && !FindRect("Card animation"), "Card animation did not finish in Play Mode.");
                next();
            }
            EditorApplication.update += Wait;
        }

        [MenuItem("Dawnwager/Check Campaign in Play Mode", true)]
        private static bool Available() => EditorApplication.isPlaying && (Find("Begin campaign") || Find("Continue campaign"));

        private static Button Find(string name) => UnityEngine.Object.FindObjectsByType<Button>()
            .FirstOrDefault(button => button.name == name && button.gameObject.activeInHierarchy);

        private static void Press(Button button)
        {
            Require(button && button.interactable, "A required button is missing or disabled.");
            Canvas.ForceUpdateCanvases();
            var center = ((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center);
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, center),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject,
                "An overlay intercepts " + button.name + ".");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
