using CapFinLoan.SharedKernel.Events;
using CapFinLoan.NotificationService.Models;

namespace CapFinLoan.NotificationService.Services
{
    /// <summary>
    /// Builds HTML-formatted notification messages based on loan status events.
    /// Each status change has a tailored message for the applicant.
    /// </summary>
    public static class NotificationBuilder
    {
        /// <summary>
        /// Builds a notification record from a status change event.
        /// Returns null if no notification needed for this transition.
        /// </summary>
        public static NotificationRecord? Build(
            LoanStatusChangedEvent evt)
        {
            var (subject, bodyContent) = evt.NewStatus switch
            {
                "Submitted" => (
                    "Application Received — CapFinLoan",
                    $"<p>Your <strong>{evt.LoanType}</strong> loan application for " +
                    $"<strong>₹{evt.LoanAmount:N0}</strong> has been received successfully.</p>" +
                    $"<table style=\"border-collapse:collapse;margin:16px 0;\">" +
                    $"<tr><td style=\"padding:4px 12px 4px 0;color:#666;\">Application ID</td>" +
                    $"<td style=\"padding:4px 0;\"><strong>{evt.ApplicationId}</strong></td></tr>" +
                    $"<tr><td style=\"padding:4px 12px 4px 0;color:#666;\">Status</td>" +
                    $"<td style=\"padding:4px 0;\"><strong>Under Review</strong></td></tr></table>" +
                    $"<p>We will review your application and get back to you shortly.</p>"
                ),
                "DocsPending" => (
                    "Documents Required — CapFinLoan",
                    $"<p>We need additional documents for your loan application.</p>" +
                    $"<div style=\"background:#fff3cd;border-left:4px solid #ffc107;padding:12px 16px;margin:16px 0;border-radius:4px;\">" +
                    $"<strong>Remarks:</strong> {evt.Remarks}</div>" +
                    $"<p>Please <a href=\"#\" style=\"color:#1a73e8;\">login to CapFinLoan</a> " +
                    $"and upload the required documents at your earliest.</p>"
                ),
                "DocsVerified" => (
                    "Documents Verified — CapFinLoan",
                    $"<p>Your documents have been <span style=\"color:#28a745;font-weight:bold;\">verified successfully</span>.</p>" +
                    $"<p>Your application is now under review by our loan officers.</p>"
                ),
                "UnderReview" => (
                    "Application Under Review — CapFinLoan",
                    $"<p>Your loan application is currently being reviewed by our team.</p>" +
                    $"<p>We will notify you once a decision is made.</p>"
                ),
                "Approved" => (
                    "Loan Approved — CapFinLoan",
                    $"<div style=\"background:#d4edda;border-left:4px solid #28a745;padding:16px;margin:16px 0;border-radius:4px;\">" +
                    $"<h3 style=\"margin:0 0 8px;color:#155724;\">Congratulations!</h3>" +
                    $"<p style=\"margin:0;\">Your <strong>{evt.LoanType}</strong> loan application has been <strong>APPROVED</strong>.</p></div>" +
                    $"<p>Please <a href=\"#\" style=\"color:#1a73e8;\">login to CapFinLoan</a> " +
                    $"to view your sanction letter and EMI details.</p>" +
                    (string.IsNullOrEmpty(evt.Remarks) ? string.Empty :
                    $"<div style=\"background:#f8f9fa;padding:12px 16px;margin:16px 0;border-radius:4px;\">" +
                    $"<strong>Remarks:</strong> {evt.Remarks}</div>")
                ),
                "Rejected" => (
                    "Application Update — CapFinLoan",
                    $"<p>We regret to inform you that your <strong>{evt.LoanType}</strong> " +
                    $"loan application could not be approved at this time.</p>" +
                    (string.IsNullOrEmpty(evt.Remarks) ? string.Empty :
                    $"<div style=\"background:#f8d7da;border-left:4px solid #dc3545;padding:12px 16px;margin:16px 0;border-radius:4px;\">" +
                    $"<strong>Reason:</strong> {evt.Remarks}</div>") +
                    $"<p>You may reapply after <strong>90 days</strong> or contact our support team for assistance.</p>"
                ),
                "Closed" => (
                    "Application Closed — CapFinLoan",
                    $"<p>Your loan application has been <strong>closed</strong>.</p>" +
                    $"<p>Thank you for choosing CapFinLoan.</p>"
                ),
                "DocumentRejected" => (
                    "Action Required: Document Rejected — CapFinLoan",
                    $"<p>One of your uploaded documents has been <span style=\"color:#dc3545;font-weight:bold;\">rejected</span> " +
                    $"by our verification team.</p>" +
                    $"<div style=\"background:#f8d7da;border-left:4px solid #dc3545;padding:12px 16px;margin:16px 0;border-radius:4px;\">" +
                    $"<strong>Document:</strong> {evt.LoanType}<br/>" +
                    $"<strong>Reason:</strong> {evt.Remarks}</div>" +
                    $"<p>Please <a href=\"#\" style=\"color:#1a73e8;\">login to CapFinLoan</a> " +
                    $"and re-upload a corrected document at your earliest convenience.</p>" +
                    $"<p>Your application status remains <strong>Docs Pending</strong> until all documents are verified.</p>"
                ),
                _ => (string.Empty, string.Empty)
            };

            if (string.IsNullOrEmpty(subject))
                return null;

            var htmlBody = WrapInHtmlTemplate(evt.ApplicantName, subject, bodyContent);

            return new NotificationRecord
            {
                ApplicationId = evt.ApplicationId,
                RecipientEmail = evt.ApplicantEmail,
                RecipientName = evt.ApplicantName,
                Subject = subject,
                Body = htmlBody,
                SentAt = DateTime.UtcNow,
                IsSuccess = true
            };
        }

        /// <summary>
        /// Wraps the email body content in a complete HTML email template
        /// with header, footer, and responsive inline styles.
        /// </summary>
        private static string WrapInHtmlTemplate(
            string recipientName, string subject, string bodyContent)
        {
            return $@"<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1.0""></head>
<body style=""margin:0;padding:0;font-family:'Segoe UI',Arial,sans-serif;background:#f4f6f8;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;margin:0 auto;background:#ffffff;"">
  <tr>
    <td style=""background:linear-gradient(135deg,#1a73e8,#0d47a1);padding:24px 32px;text-align:center;"">
      <h1 style=""margin:0;color:#ffffff;font-size:24px;font-weight:600;"">CapFinLoan</h1>
      <p style=""margin:4px 0 0;color:#bbdefb;font-size:13px;"">Loan Management System</p>
    </td>
  </tr>
  <tr>
    <td style=""padding:32px;"">
      <p style=""margin:0 0 16px;font-size:16px;color:#333;"">Dear <strong>{recipientName}</strong>,</p>
      {bodyContent}
    </td>
  </tr>
  <tr>
    <td style=""background:#f8f9fa;padding:20px 32px;border-top:1px solid #e9ecef;"">
      <p style=""margin:0;font-size:13px;color:#666;text-align:center;"">
        This is an automated notification from CapFinLoan.<br/>
        Please do not reply to this email.<br/>
        &copy; {DateTime.UtcNow.Year} CapFinLoan — All rights reserved.
      </p>
    </td>
  </tr>
</table>
</body>
</html>";
        }
    }
}
