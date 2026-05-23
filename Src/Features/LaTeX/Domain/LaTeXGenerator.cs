using System;
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
        sb.AppendLine("\\usepackage[utf8]{inputenc}");
        sb.AppendLine("\\usepackage{graphicx}");
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
            sb.AppendLine("\\textbf{Note:}");
            sb.AppendLine(FormatContent(q.AnswerText));
            await AppendMediaLaTeXAsync(q.Id, MediaStatus.ANSWER, sb, imagesDestDir);
        }
        else
        {
            sb.AppendLine($"\\textbf{{Question:}} {FormatContent(q.QuestionText)}");
            await AppendMediaLaTeXAsync(q.Id, MediaStatus.QUESTION, sb, imagesDestDir);

            sb.AppendLine("\\vspace{0.2cm}");
            sb.AppendLine($"\\textbf{{Answer:}} {FormatContent(q.AnswerText)}");
            await AppendMediaLaTeXAsync(q.Id, MediaStatus.ANSWER, sb, imagesDestDir);
        }

        sb.AppendLine("\\hrulefill");
        sb.AppendLine("\\vspace{0.4cm}");
        sb.AppendLine("\\end{minipage}");
    }

    private async Task AppendMediaLaTeXAsync(int questionId, MediaStatus status, StringBuilder sb, string imagesDestDir)
    {
        var mediaList = await _mediaRepository.GetByQuestionIdAsync(questionId);
        var filteredMedia = mediaList.Where(m => m.Status == status).ToList();

        var config = Locator.Current.GetService<IOptions<AppConfig>>()!.Value;
        var mediaDir = config.ResolvedMediaDirectoryPath;

        foreach (var m in filteredMedia)
        {
            var srcPath = Path.Combine(mediaDir, m.Filename);
            if (File.Exists(srcPath))
            {
                var destPath = Path.Combine(imagesDestDir, m.Filename);
                File.Copy(srcPath, destPath, overwrite: true);

                sb.AppendLine("\\begin{center}");
                // LaTeX path uses forward slashes
                sb.AppendLine($"\\includegraphics[width=0.8\\textwidth]{{images/{m.Filename}}}");
                sb.AppendLine("\\end{center}");
            }
        }
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
