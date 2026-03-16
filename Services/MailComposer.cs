using System.Text.RegularExpressions;
using MailDispatcher.Worker.Models;
using MimeKit;

namespace MailDispatcher.Worker.Services;

public sealed class MailComposer
{
    private static readonly Regex CidRegex =
        new("""src\s*=\s*["']cid:([^"' >]+)["']""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ComposeResult Compose(MailQueueItem item, MailProfileConfig profile, string inlineImagesPath)
    {
        var result = new ComposeResult();
        var message = new MimeMessage();

        var fromAddress = string.IsNullOrWhiteSpace(item.De) ? profile.De : item.De!;
        message.From.Add(new MailboxAddress(profile.DeMostrar ?? string.Empty, fromAddress));

        AddRecipients(message.To, item.A);
        AddRecipients(message.Cc, item.Copia);
        AddRecipients(message.Bcc, item.CopiaOculta);

        message.Subject = item.Asunto ?? string.Empty;

        var builder = new BodyBuilder
        {
            HtmlBody = item.Cuerpo ?? string.Empty
        };

        AddInlineResources(builder, item.Cuerpo ?? string.Empty, inlineImagesPath, result);
        AddAttachments(builder, item.Adjuntos, result);

        message.Body = builder.ToMessageBody();
        result.Message = message;

        return result;
    }

    private static void AddRecipients(InternetAddressList list, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            list.Add(MailboxAddress.Parse(part));
        }
    }

    private static void AddInlineResources(BodyBuilder builder, string html, string inlineImagesPath, ComposeResult result)
    {
        var matches = CidRegex.Matches(html);

        foreach (Match match in matches)
        {
            var cid = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(cid))
                continue;

            result.CidsDetected.Add(cid);

            var path = Path.Combine(inlineImagesPath, cid);

            if (!File.Exists(path))
            {
                result.InlineMissing.Add(path);
                continue;
            }

            var resource = builder.LinkedResources.Add(path);
            resource.ContentId = cid;
            result.InlineFound.Add(path);
        }
    }

    private static void AddAttachments(BodyBuilder builder, string? adjuntos, ComposeResult result)
    {
        if (string.IsNullOrWhiteSpace(adjuntos))
            return;

        foreach (var file in adjuntos.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!File.Exists(file))
            {
                result.AttachmentsMissing.Add(file);
                continue;
            }

            builder.Attachments.Add(file);
            result.AttachmentsFound.Add(file);
        }
    }
}