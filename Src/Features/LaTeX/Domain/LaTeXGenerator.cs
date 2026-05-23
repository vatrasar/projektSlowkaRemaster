using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Splat;
using ProjektSlowkaRemasterd.Src.Core.Config;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;
using ProjektSlowkaRemasterd.Src.Core.Domain.Enums;

namespace ProjektSlowkaRemasterd.Src.Features.LaTeX.Domain;

using Question = ProjektSlowkaRemasterd.Src.Core.Domain.Models.Question;


public class LaTeXGenerator
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITopicRepository _topicRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IMediaRepository _mediaRepository;

    public LaTeXGenerator(
        ICategoryRepository categoryRepository,
        ITopicRepository topicRepository,
        ISectionRepository sectionRepository,
        IQuestionRepository questionRepository,
        IMediaRepository mediaRepository)
    {
        _categoryRepository = categoryRepository;
        _topicRepository = topicRepository;
        _sectionRepository = sectionRepository;
        _questionRepository = questionRepository;
        _mediaRepository = mediaRepository;
    }

    /// <summary>
    /// Exports all topics, sections, and questions under the specified category into a LaTeX document.
    /// Invoked by:
    /// - <see cref="ProjektSlowkaRemasterd.Src.Features.Category.UI.Screens.Manage.ManageViewModel"/>
    /// </summary>
    /// <param name="categoryId">The ID of the category to export.</param>
    /// <param name="exportFolderPath">The target directory path to write the LaTeX document and its resources.</param>
    /// <returns>A Task representing the asynchronous export operation.</returns>
    public async Task ExportCategoryAsync(int categoryId, string exportFolderPath)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId);
        if (category == null) throw new ArgumentException("Category not found");

        if (!Directory.Exists(exportFolderPath))
        {
            Directory.CreateDirectory(exportFolderPath);
        }

        var imagesDestDir = Path.Combine(exportFolderPath, "images");
        if (!Directory.Exists(imagesDestDir))
        {
            Directory.CreateDirectory(imagesDestDir);
        }

        var topics = (await _topicRepository.GetByCategoryIdAsync(categoryId)).ToList();
        var questions = (await _questionRepository.GetByCategoryIdAsync(categoryId))
            .Where(q => q.Status != QuestionStatus.TO_ARCHIVE)
            .ToList();

        var sb = new StringBuilder();
        
        // Preamble
        sb.AppendLine("\\documentclass{article}");
        sb.AppendLine("\\usepackage[T1]{fontenc}");
        sb.AppendLine("\\usepackage[utf8]{inputenc}");
        sb.AppendLine("\\usepackage[polish]{babel}");
        sb.AppendLine("\\usepackage{lmodern}");
        sb.AppendLine("\\usepackage{graphicx}");
        sb.AppendLine("\\usepackage{caption}");
        sb.AppendLine("\\usepackage{listings}");
        sb.AppendLine("\\usepackage{hyperref}");
        sb.AppendLine("\\usepackage{geometry}");
        sb.AppendLine("\\geometry{a4paper, margin=1in}");
        sb.AppendLine($"\\title{{{EscapeLaTeX(category.Name)}}}");
        sb.AppendLine("\\author{Spaced Repetition Flashcard System}");
        sb.AppendLine($"\\date{{\\today}}");
        sb.AppendLine("\\begin{document}");
        sb.AppendLine("\\maketitle");
        sb.AppendLine("\\tableofcontents");
        sb.AppendLine("\\newpage");

        // 1. Export questions directly under category (no topic)
        var categoryQuestions = questions.Where(q => q.TopicId == null).ToList();
        if (categoryQuestions.Count > 0)
        {
            sb.AppendLine("\\section{General Questions}");
            foreach (var q in categoryQuestions)
            {
                await AppendQuestionLaTeXAsync(q, sb, imagesDestDir);
            }
        }

        // 2. Export topics
        foreach (var topic in topics)
        {
            sb.AppendLine($"\\section{{{EscapeLaTeX(topic.Name)}}}");

            // Questions in topic with no section
            var topicQuestions = questions.Where(q => q.TopicId == topic.Id && q.SectionId == null).ToList();
            foreach (var q in topicQuestions)
            {
                await AppendQuestionLaTeXAsync(q, sb, imagesDestDir);
            }

            // Sections in topic
            var sections = (await _sectionRepository.GetByTopicIdAsync(topic.Id)).ToList();
            foreach (var sec in sections)
            {
                var secQuestions = questions.Where(q => q.SectionId == sec.Id).ToList();
                if (secQuestions.Count > 0)
                {
                    sb.AppendLine($"\\subsection{{{EscapeLaTeX(sec.Name)}}}");
                    foreach (var q in secQuestions)
                    {
                        await AppendQuestionLaTeXAsync(q, sb, imagesDestDir);
                    }
                }
            }
        }

        sb.AppendLine("\\end{document}");

        var texFilePath = Path.Combine(exportFolderPath, "document.tex");
        await File.WriteAllTextAsync(texFilePath, sb.ToString(), Encoding.UTF8);
    }

    private async Task AppendQuestionLaTeXAsync(Question q, StringBuilder sb, string imagesDestDir)
    {
        sb.AppendLine("\\begin{minipage}{\\textwidth}");
        sb.AppendLine("\\vspace{0.4cm}");
        
        if (q.IsNotion)
        {
            var (noteText, mediaLatex) = await FormatContentWithMediaRefsAsync(q.Id, MediaStatus.ANSWER, q.AnswerText, imagesDestDir);
            sb.AppendLine("\\subsubsection*{Note}");
            sb.AppendLine(noteText);
            sb.AppendLine(mediaLatex);
        }
        else
        {
            var (questionText, qMediaLatex) = await FormatContentWithMediaRefsAsync(q.Id, MediaStatus.QUESTION, q.QuestionText, imagesDestDir);
            sb.AppendLine("\\subsubsection*{Question}");
            sb.AppendLine(questionText);
            sb.AppendLine(qMediaLatex);

            sb.AppendLine("\\vspace{0.2cm}");

            var (answerText, aMediaLatex) = await FormatContentWithMediaRefsAsync(q.Id, MediaStatus.ANSWER, q.AnswerText, imagesDestDir);
            sb.AppendLine("\\subsubsection*{Answer}");
            sb.AppendLine(answerText);
            sb.AppendLine(aMediaLatex);
        }

        sb.AppendLine("\\hrulefill");
        sb.AppendLine("\\vspace{0.4cm}");
        sb.AppendLine("\\end{minipage}");
    }

    private async Task<(string Text, string MediaLatex)> FormatContentWithMediaRefsAsync(
        int questionId,
        MediaStatus status,
        string contentText,
        string imagesDestDir)
    {
        var mediaList = await _mediaRepository.GetByQuestionIdAsync(questionId);
        var filteredMedia = mediaList.Where(m => m.Status == status).ToList();

        var config = Locator.Current.GetService<IOptions<AppConfig>>()!.Value;
        var mediaDir = config.ResolvedMediaDirectoryPath;

        var mediaSb = new StringBuilder();
        var refList = new List<string>();

        foreach (var m in filteredMedia)
        {
            var srcPath = Path.Combine(mediaDir, m.Filename);
            if (File.Exists(srcPath))
            {
                var destPath = Path.Combine(imagesDestDir, m.Filename);
                File.Copy(srcPath, destPath, overwrite: true);

                var label = $"fig:media_{m.Id}";
                refList.Add($"\\ref{{{label}}}");

                mediaSb.AppendLine("        \\begin{center}");
                mediaSb.AppendLine($"            \\includegraphics[width=0.8\\textwidth]{{images/{m.Filename}}}");
                mediaSb.AppendLine("            \\captionof{figure}{Ilustracja}");
                mediaSb.AppendLine($"            \\label{{{label}}}");
                mediaSb.AppendLine("        \\end{center}");
            }
        }

        var formattedContent = FormatContent(contentText);
        if (refList.Count > 0)
        {
            var refsString = string.Join(", ", refList);
            if (formattedContent.EndsWith(Environment.NewLine))
            {
                formattedContent = formattedContent.TrimEnd();
            }
            formattedContent += $" (zobacz rys. {refsString})";
        }

        return (formattedContent, mediaSb.ToString());
    }

    private string FormatContent(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new StringBuilder();
        int index = 0;

        while (index < text.Length)
        {
            int scIndex = text.IndexOf("*sc", index);
            if (scIndex == -1)
            {
                sb.Append(EscapeLaTeX(text.Substring(index)));
                break;
            }

            // Append everything before *sc escaped
            sb.Append(EscapeLaTeX(text.Substring(index, scIndex - index)));

            int ecIndex = text.IndexOf("*ec", scIndex + 3);
            if (ecIndex == -1)
            {
                // Unclosed tag, treat rest as plain text
                sb.Append(EscapeLaTeX(text.Substring(scIndex)));
                break;
            }

            // Extract code
            var code = text.Substring(scIndex + 3, ecIndex - (scIndex + 3));
            sb.AppendLine();
            sb.AppendLine("\\begin{verbatim}");
            sb.Append(code);
            sb.AppendLine("\\end{verbatim}");

            index = ecIndex + 3;
        }

        return sb.ToString();
    }

    private string EscapeLaTeX(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        return text
            .Replace("\\", "\\textbackslash{}")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("$", "\\$")
            .Replace("&", "\\&")
            .Replace("#", "\\#")
            .Replace("_", "\\_")
            .Replace("%", "\\%")
            .Replace("~", "\\textasciitilde{}")
            .Replace("^", "\\textasciicircum{}");
    }
}
