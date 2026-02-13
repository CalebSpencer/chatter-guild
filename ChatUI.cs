using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatUI : MonoBehaviour
{
    public TMP_Text headerText;
    public TMP_Text footerText;

    public TMP_InputField input;
    public Button sendButton;

    public Transform content;
    public GameObject messagePrefab;
    public GameObject systemPrefab;

    public event Action<string> OnSend;

    void Start()
    {
        sendButton.onClick.AddListener(() =>
        {
            var text = input.text;
            input.text = "";
            OnSend?.Invoke(text);
        });
    }

    public void SetHeader(string s) { if (headerText) headerText.text = s; }
    public void SetFooter(string s) { if (footerText) footerText.text = s; }

    public void AddMessage(string who, string text)
    {
        var go = Instantiate(messagePrefab, content);
        var t = go.GetComponentInChildren<TMP_Text>();
        t.text = $"<b>{who}:</b> {text}";
    }

    public void AddSystem(string text)
    {
        var go = Instantiate(systemPrefab ? systemPrefab : messagePrefab, content);
        var t = go.GetComponentInChildren<TMP_Text>();
        t.text = $"<i>{text}</i>";
    }
}