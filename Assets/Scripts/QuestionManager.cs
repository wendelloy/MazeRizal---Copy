using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System.IO;

public class QuestionManager
{
    private List<Question> questions;
    private List<Question> usedQuestions; // Track used questions

    public void LoadQuestions(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            questions = JsonConvert.DeserializeObject<List<Question>>(json);
            usedQuestions = new List<Question>(); // Initialize used questions list
        }
        else
        {
            questions = new List<Question>();
            usedQuestions = new List<Question>();
        }
    }

    public Question GetRandomQuestion()
    {
        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("No questions loaded!");
            return null;
        }

        // Include both available and used questions for selection
        var availableQuestions = questions.FindAll(q => !usedQuestions.Contains(q));

        if (availableQuestions.Count == 0)
        {
            Debug.LogWarning("No more available questions!");
            // Allow all questions to be used again (optional)
            usedQuestions.Clear();
            availableQuestions = new List<Question>(questions);
        }

        // Pick a random question
        int index = Random.Range(0, availableQuestions.Count);
        return availableQuestions[index];
    }

    public void MarkQuestionUsed(Question question)
    {
        if (!usedQuestions.Contains(question))
        {
            usedQuestions.Add(question);
        }
    }
}
