using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjektSlowkaRemasterd.Src.Features.Question.Domain.Services;

public class ParsedQuestion
{
    public string QuestionText { get; set; } = string.Empty;
    public string AnswerText { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int QuestionNumber { get; set; }
}

public static class BulkQuestionParser
{
    private static readonly Regex QuestionRegex = new(@"^Q(\d+):", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex AnswerRegex = new(@"^A(\d+):", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private class ParseContext
    {
        public int BlockStartLine = 1;
        public int CurrentLineNumber;
        public readonly StringBuilder CurrentQuestionText = new();
        public readonly StringBuilder CurrentAnswerText = new();
        public int? CurrentQNum;
        public int? CurrentANum;
        public int ActiveField; // 0=None, 1=Question, 2=Answer
        public readonly List<ParsedQuestion> Questions = new();
    }

    public static List<ParsedQuestion> Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new List<ParsedQuestion>();
        }

        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var context = new ParseContext();

        foreach (var line in lines)
        {
            ProcessLine(line, context);
        }

        // Validate and add the last block if it contains any data
        if (context.CurrentQNum != null || context.CurrentANum != null || 
            context.CurrentQuestionText.Length > 0 || context.CurrentAnswerText.Length > 0)
        {
            ValidateAndAddQuestion(context);
        }

        return context.Questions;
    }

    private static void ProcessLine(string line, ParseContext context)
    {
        context.CurrentLineNumber++;
        var trimmedLine = line.Trim();

        if (trimmedLine == "---")
        {
            HandleBlockSeparator(context);
            return;
        }

        HandleInsideBlock(line, trimmedLine, context);
    }

    private static void HandleBlockSeparator(ParseContext context)
    {
        // Only validate and add if there is some content in the current block
        if (context.CurrentQNum != null || context.CurrentANum != null || 
            context.CurrentQuestionText.Length > 0 || context.CurrentAnswerText.Length > 0)
        {
            ValidateAndAddQuestion(context);
        }

        // Reset context for the next block
        context.CurrentQuestionText.Clear();
        context.CurrentAnswerText.Clear();
        context.CurrentQNum = null;
        context.CurrentANum = null;
        context.ActiveField = 0;
        context.BlockStartLine = context.CurrentLineNumber + 1; // next block starts on next line
    }

    private static void HandleInsideBlock(string line, string trimmedLine, ParseContext context)
    {
        var qMatch = QuestionRegex.Match(trimmedLine);
        if (qMatch.Success)
        {
            HandleQuestionMatch(qMatch, trimmedLine, context);
            return;
        }

        var aMatch = AnswerRegex.Match(trimmedLine);
        if (aMatch.Success)
        {
            HandleAnswerMatch(aMatch, trimmedLine, context);
            return;
        }

        HandleFieldContent(line, trimmedLine, context);
    }

    private static void HandleQuestionMatch(Match qMatch, string trimmedLine, ParseContext context)
    {
        if (context.CurrentQNum != null)
        {
            throw new FormatException($"Format error at line {context.CurrentLineNumber}: Multiple Question fields defined in a single block.");
        }
        context.CurrentQNum = int.Parse(qMatch.Groups[1].Value);
        context.ActiveField = 1;

        var remainingText = trimmedLine.Substring(qMatch.Length).Trim();
        if (!string.IsNullOrEmpty(remainingText))
        {
            context.CurrentQuestionText.AppendLine(remainingText);
        }
    }

    private static void HandleAnswerMatch(Match aMatch, string trimmedLine, ParseContext context)
    {
        if (context.CurrentANum != null)
        {
            throw new FormatException($"Format error at line {context.CurrentLineNumber}: Multiple Answer fields defined in a single block.");
        }
        context.CurrentANum = int.Parse(aMatch.Groups[1].Value);
        context.ActiveField = 2;

        var remainingText = trimmedLine.Substring(aMatch.Length).Trim();
        if (!string.IsNullOrEmpty(remainingText))
        {
            context.CurrentAnswerText.AppendLine(remainingText);
        }
    }

    private static void HandleFieldContent(string line, string trimmedLine, ParseContext context)
    {
        if (context.ActiveField == 1)
        {
            context.CurrentQuestionText.AppendLine(line);
        }
        else if (context.ActiveField == 2)
        {
            context.CurrentAnswerText.AppendLine(line);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                throw new FormatException($"Format error at line {context.CurrentLineNumber}: Text found before specifying the field (e.g. Q1:).");
            }
        }
    }

    private static void ValidateAndAddQuestion(ParseContext context)
    {
        var qText = context.CurrentQuestionText.ToString().Trim();
        var aText = context.CurrentAnswerText.ToString().Trim();
        var startLine = context.BlockStartLine;
        var qNum = context.CurrentQNum;
        var aNum = context.CurrentANum;

        if (qNum == null)
        {
            throw new FormatException($"Format error in block starting at line {startLine}: Question field (Q[num]:) is missing.");
        }
        if (aNum == null)
        {
            throw new FormatException($"Format error in block starting at line {startLine}: Answer field (A[num]:) is missing.");
        }

        if (qNum != aNum)
        {
            throw new FormatException($"Format error in block starting at line {startLine}: Question number ({qNum}) does not match Answer number ({aNum}).");
        }

        if (string.IsNullOrWhiteSpace(qText))
        {
            throw new FormatException($"Format error in block starting at line {startLine}: Question text cannot be empty.");
        }
        if (string.IsNullOrWhiteSpace(aText))
        {
            throw new FormatException($"Format error in block starting at line {startLine}: Answer text cannot be empty.");
        }

        if (aText.Length > 10000)
        {
            throw new FormatException($"Format error in block starting at line {startLine}: Answer length cannot exceed 10000 characters (currently {aText.Length}).");
        }

        context.Questions.Add(new ParsedQuestion
        {
            QuestionText = qText,
            AnswerText = aText,
            LineNumber = startLine,
            QuestionNumber = qNum.Value
        });
    }
}
