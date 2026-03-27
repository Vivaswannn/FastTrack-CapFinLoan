using CapFinLoan.DocumentService.DTOs.Requests;
using CapFinLoan.DocumentService.Models;
using CapFinLoan.DocumentService.Repositories.Interfaces;
using CapFinLoan.DocumentService.Validators;
using CapFinLoan.SharedKernel.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CapFinLoan.DocumentService.Tests
{
    [TestFixture]
    public class DocumentServiceTests
    {
        private Mock<IDocumentRepository> _repositoryMock = null!;
        private Mock<IWebHostEnvironment> _environmentMock = null!;
        private Mock<ILogger<Services.DocumentService>> _loggerMock = null!;
        private Services.DocumentService _service = null!;

        private readonly Guid _userId        = Guid.NewGuid();
        private readonly Guid _applicationId = Guid.NewGuid();

        [SetUp]
        public void SetUp()
        {
            _repositoryMock   = new Mock<IDocumentRepository>();
            _environmentMock  = new Mock<IWebHostEnvironment>();
            _loggerMock       = new Mock<ILogger<Services.DocumentService>>();

            // Point WebRootPath at the system temp dir so path resolution works
            _environmentMock.Setup(e => e.WebRootPath)
                .Returns(Path.GetTempPath());

            _service = new Services.DocumentService(
                _repositoryMock.Object,
                _environmentMock.Object,
                _loggerMock.Object);
        }

        // ── Helper: build a mock IFormFile ───────────────────────────────────

        private static IFormFile CreateMockFile(
            string fileName,
            long size,
            string contentType = "application/octet-stream")
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(size);
            fileMock.Setup(f => f.ContentType).Returns(contentType);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return fileMock.Object;
        }

        private UploadDocumentDto CreateUploadDto() =>
            new UploadDocumentDto
            {
                ApplicationId = _applicationId,
                DocumentType  = DocumentType.AadhaarCard
            };

        private Document CreateSampleDocument(bool isReplaced = false) =>
            new Document
            {
                DocumentId    = Guid.NewGuid(),
                ApplicationId = _applicationId,
                UserId        = _userId,
                DocumentType  = DocumentType.AadhaarCard,
                FileName      = "aadhar.pdf",
                StoredFileName = Guid.NewGuid() + ".pdf",
                FilePath      = "uploads/2026/01/sample.pdf",
                FileExtension = ".pdf",
                FileSizeBytes = 1_048_576,
                IsVerified    = false,
                IsReplaced    = isReplaced,
                UploadedAt    = DateTime.UtcNow
            };

        // ── TC06: Upload valid PDF within 5 MB ───────────────────────────────

        [Test]
        public async Task UploadDocumentAsync_ValidPdfWithin5MB_ReturnsDocumentResponse()
        {
            // Arrange
            var file = CreateMockFile("aadhar.pdf", 1_048_576, "application/pdf");
            var dto  = CreateUploadDto();

            _repositoryMock
                .Setup(r => r.GetByApplicationIdAndTypeAsync(_applicationId, DocumentType.AadhaarCard))
                .ReturnsAsync((Document?)null);

            var savedDoc = CreateSampleDocument();
            _repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<Document>()))
                .ReturnsAsync(savedDoc);

            // Act
            var result = await _service.UploadDocumentAsync(file, dto, _userId);

            // Assert
            result.Should().NotBeNull();
            result.FileName.Should().Be("aadhar.pdf");
            result.IsVerified.Should().BeFalse();
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Document>()), Times.Once);
        }

        // ── TC07: Upload unsupported file type ───────────────────────────────

        [Test]
        public async Task UploadDocumentAsync_UnsupportedFileType_ThrowsArgumentException()
        {
            // Arrange
            var file = CreateMockFile("document.exe", 1_048_576);
            var dto  = CreateUploadDto();

            // Act
            var act = async () => await _service.UploadDocumentAsync(file, dto, _userId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*Invalid file type*");

            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Document>()), Times.Never);
        }

        // ── Upload file exceeding 5 MB ───────────────────────────────────────

        [Test]
        public async Task UploadDocumentAsync_FileTooLarge_ThrowsArgumentException()
        {
            // Arrange — 6 MB
            var file = CreateMockFile("large.pdf", 6_291_456, "application/pdf");
            var dto  = CreateUploadDto();

            // Act
            var act = async () => await _service.UploadDocumentAsync(file, dto, _userId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*5MB*");

            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Document>()), Times.Never);
        }

        // ── Re-upload replaces existing document of same type ────────────────

        [Test]
        public async Task UploadDocumentAsync_ReplacesExistingDocOfSameType()
        {
            // Arrange
            var file        = CreateMockFile("aadhar_new.pdf", 512_000, "application/pdf");
            var dto         = CreateUploadDto();
            var existingDoc = CreateSampleDocument();
            Document? capturedExisting = null;

            _repositoryMock
                .Setup(r => r.GetByApplicationIdAndTypeAsync(_applicationId, DocumentType.AadhaarCard))
                .ReturnsAsync(existingDoc);

            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Document>()))
                .Callback<Document>(d => capturedExisting = d)
                .ReturnsAsync((Document d) => d);

            var newDoc = CreateSampleDocument();
            _repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<Document>()))
                .ReturnsAsync(newDoc);

            // Act
            await _service.UploadDocumentAsync(file, dto, _userId);

            // Assert
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Document>()), Times.Once);
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Document>()), Times.Once);
            capturedExisting.Should().NotBeNull();
            capturedExisting!.IsReplaced.Should().BeTrue();
        }

        // ── TC09: Verify document marks it as verified ───────────────────────

        [Test]
        public async Task VerifyDocumentAsync_ValidDocument_MarksAsVerified()
        {
            // Arrange
            var docId       = Guid.NewGuid();
            var adminEmail  = "admin@capfinloan.com";
            var existingDoc = CreateSampleDocument();
            existingDoc.DocumentId = docId;

            _repositoryMock
                .Setup(r => r.GetByIdAsync(docId))
                .ReturnsAsync(existingDoc);

            _repositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<Document>()))
                .ReturnsAsync((Document d) => d);

            var dto = new VerifyDocumentDto { IsVerified = true, VerificationRemarks = null };

            // Act
            var result = await _service.VerifyDocumentAsync(docId, dto, adminEmail);

            // Assert
            result.IsVerified.Should().BeTrue();
            result.VerifiedBy.Should().Be(adminEmail);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Document>()), Times.Once);
        }

        // ── Rejection without remarks fails FluentValidation ────────────────

        [Test]
        public void VerifyDocumentDto_RejectionWithoutRemarks_FailsValidation()
        {
            // Arrange
            var dto       = new VerifyDocumentDto { IsVerified = false, VerificationRemarks = string.Empty };
            var validator = new VerifyDocumentValidator();

            // Act
            var result = validator.Validate(dto);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.ErrorMessage.Contains("reason for rejecting"));
        }

        // ── Applicant sees only own documents ────────────────────────────────

        [Test]
        public async Task GetDocumentsByApplicationAsync_ApplicantRole_OnlyOwnDocs()
        {
            // Arrange
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid(); // the requester
            var docs = new List<Document>
            {
                new Document { DocumentId = Guid.NewGuid(), UserId = userId1,
                               ApplicationId = _applicationId, DocumentType = DocumentType.PAN,
                               FileName = "pan.pdf", FileExtension = ".pdf", IsReplaced = false,
                               UploadedAt = DateTime.UtcNow }
            };

            _repositoryMock
                .Setup(r => r.GetByApplicationIdAsync(_applicationId))
                .ReturnsAsync(docs);

            // Act — userId2 is the requester, documents belong to userId1
            var result = await _service.GetDocumentsByApplicationAsync(
                _applicationId, userId2, "Applicant");

            // Assert: applicant cannot see another user's documents
            result.Should().BeEmpty();
        }

        // ── Admin sees all documents ──────────────────────────────────────────

        [Test]
        public async Task GetDocumentsByApplicationAsync_AdminRole_SeesAllDocs()
        {
            // Arrange
            var anyUserId = Guid.NewGuid();
            var docs = new List<Document>
            {
                new Document { DocumentId = Guid.NewGuid(), UserId = Guid.NewGuid(),
                               ApplicationId = _applicationId, DocumentType = DocumentType.PAN,
                               FileName = "pan.pdf", FileExtension = ".pdf", IsReplaced = false,
                               UploadedAt = DateTime.UtcNow }
            };

            _repositoryMock
                .Setup(r => r.GetByApplicationIdAsync(_applicationId))
                .ReturnsAsync(docs);

            // Act
            var result = await _service.GetDocumentsByApplicationAsync(
                _applicationId, anyUserId, "Admin");

            // Assert
            result.Should().HaveCount(1);
            _repositoryMock.Verify(r => r.GetByApplicationIdAsync(_applicationId), Times.Once);
        }

        // ── Upload null file throws ArgumentException ─────────────────────────

        [Test]
        public async Task UploadDocumentAsync_NullFile_ThrowsArgumentException()
        {
            var dto = CreateUploadDto();
            var act = async () => await _service.UploadDocumentAsync(null!, dto, _userId);
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*No file*");
        }

        // ── GetDocumentByIdAsync returns document by ID ───────────────────────

        [Test]
        public async Task GetDocumentByIdAsync_ValidDocument_ReturnsDto()
        {
            var doc = CreateSampleDocument();
            _repositoryMock.Setup(r => r.GetByIdAsync(doc.DocumentId)).ReturnsAsync(doc);

            var result = await _service.GetDocumentByIdAsync(doc.DocumentId, _userId, "Admin");

            result.Should().NotBeNull();
            result.DocumentId.Should().Be(doc.DocumentId);
        }

        // ── GetDocumentByIdAsync not found ────────────────────────────────────

        [Test]
        public async Task GetDocumentByIdAsync_NotFound_ThrowsKeyNotFoundException()
        {
            var docId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(docId)).ReturnsAsync((Document?)null);

            var act = async () => await _service.GetDocumentByIdAsync(docId, _userId, "Applicant");

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ── GetDocumentByIdAsync applicant denied for others docs ─────────────

        [Test]
        public async Task GetDocumentByIdAsync_ApplicantAccessingOthersDocs_ThrowsKeyNotFound()
        {
            var doc = CreateSampleDocument();
            doc.UserId = Guid.NewGuid(); // different user
            _repositoryMock.Setup(r => r.GetByIdAsync(doc.DocumentId)).ReturnsAsync(doc);

            var act = async () => await _service.GetDocumentByIdAsync(doc.DocumentId, _userId, "Applicant");

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ── GetFilePathAsync returns path ─────────────────────────────────────

        [Test]
        public async Task GetFilePathAsync_ValidDocument_ReturnsFilePath()
        {
            var doc = CreateSampleDocument();
            _repositoryMock.Setup(r => r.GetByIdAsync(doc.DocumentId)).ReturnsAsync(doc);

            var path = await _service.GetFilePathAsync(doc.DocumentId, _userId, "Admin");

            path.Should().NotBeNullOrEmpty();
            path.Should().Be(doc.FilePath);
        }

        // ── GetFilePathAsync blocks wrong user ────────────────────────────────

        [Test]
        public async Task GetFilePathAsync_ApplicantWrongUser_ThrowsKeyNotFound()
        {
            var doc = CreateSampleDocument();
            doc.UserId = Guid.NewGuid(); // different user
            _repositoryMock.Setup(r => r.GetByIdAsync(doc.DocumentId)).ReturnsAsync(doc);

            var act = async () => await _service.GetFilePathAsync(doc.DocumentId, _userId, "Applicant");

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ── GetFilePathAsync not found ────────────────────────────────────────

        [Test]
        public async Task GetFilePathAsync_NotFound_ThrowsKeyNotFoundException()
        {
            var docId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(docId)).ReturnsAsync((Document?)null);

            var act = async () => await _service.GetFilePathAsync(docId, _userId, "Admin");

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ── VerifyDocumentAsync with IsVerified=false ──────────────────────────

        [Test]
        public async Task VerifyDocumentAsync_Rejection_SetsFieldsCorrectly()
        {
            var docId = Guid.NewGuid();
            var doc = CreateSampleDocument();
            doc.DocumentId = docId;

            _repositoryMock.Setup(r => r.GetByIdAsync(docId)).ReturnsAsync(doc);
            _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Document>())).ReturnsAsync((Document d) => d);

            var dto = new VerifyDocumentDto
            {
                IsVerified = false,
                VerificationRemarks = "Blurry image"
            };

            var result = await _service.VerifyDocumentAsync(docId, dto, "admin@capfinloan.com");

            result.IsVerified.Should().BeFalse();
            result.VerificationRemarks.Should().Be("Blurry image");
            result.VerifiedBy.Should().Be("admin@capfinloan.com");
        }

        // ── VerifyDocumentAsync not found ─────────────────────────────────────

        [Test]
        public async Task VerifyDocumentAsync_NotFound_ThrowsKeyNotFoundException()
        {
            var docId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync(docId)).ReturnsAsync((Document?)null);

            var dto = new VerifyDocumentDto { IsVerified = true };
            var act = async () => await _service.VerifyDocumentAsync(docId, dto, "admin@capfinloan.com");

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // ── GetAllDocumentsByApplicationAsync ─────────────────────────────────

        [Test]
        public async Task GetAllDocumentsByApplicationAsync_ReturnsAllDocs()
        {
            var docs = new List<Document> { CreateSampleDocument(), CreateSampleDocument() };
            _repositoryMock.Setup(r => r.GetAllByApplicationIdAsync(_applicationId)).ReturnsAsync(docs);

            var result = await _service.GetAllDocumentsByApplicationAsync(_applicationId);

            result.Should().HaveCount(2);
        }

        // ── FileHelper Tests ──────────────────────────────────────────────────

        [TestCase(".pdf", true)]
        [TestCase(".jpg", true)]
        [TestCase(".jpeg", true)]
        [TestCase(".png", true)]
        [TestCase(".PDF", true)]
        [TestCase(".exe", false)]
        [TestCase(".docx", false)]
        [TestCase(".zip", false)]
        [TestCase("", false)]
        public void FileHelper_IsValidExtension_ReturnsCorrectResult(string ext, bool expected)
        {
            var result = Helpers.FileHelper.IsValidExtension($"file{ext}");
            result.Should().Be(expected);
        }

        [Test]
        public void FileHelper_IsValidFileSize_Within5MB_ReturnsTrue()
        {
            Helpers.FileHelper.IsValidFileSize(1_048_576).Should().BeTrue();
            Helpers.FileHelper.IsValidFileSize(5_242_880).Should().BeTrue();
        }

        [Test]
        public void FileHelper_IsValidFileSize_Over5MB_ReturnsFalse()
        {
            Helpers.FileHelper.IsValidFileSize(5_242_881).Should().BeFalse();
            Helpers.FileHelper.IsValidFileSize(10_000_000).Should().BeFalse();
        }

        [TestCase(1_048_576L, "1.0 MB")]
        [TestCase(2_621_440L, "2.5 MB")]
        [TestCase(512_000L, "500 KB")]
        [TestCase(500L, "500 B")]
        public void FileHelper_FormatFileSize_ReturnsCorrectString(long bytes, string expected)
        {
            var result = Helpers.FileHelper.FormatFileSize(bytes);
            result.Should().Be(expected);
        }

        [Test]
        public void FileHelper_GenerateStoredFileName_ReturnsUniqueGuidName()
        {
            var result = Helpers.FileHelper.GenerateStoredFileName("test.pdf");
            result.Should().EndWith(".pdf");
            result.Length.Should().BeGreaterThan(36); // GUID + extension
        }

        [Test]
        public void FileHelper_GetRelativePath_ContainsYearMonth()
        {
            var now = DateTime.UtcNow;
            var result = Helpers.FileHelper.GetRelativePath("test.pdf");
            result.Should().Contain($"uploads/{now.Year}/");
        }

        // ── VerifyDocumentValidator tests ────────────────────────────────────

        [Test]
        public void VerifyDocumentValidator_ApprovalWithoutRemarks_Passes()
        {
            var dto = new VerifyDocumentDto { IsVerified = true, VerificationRemarks = null };
            var validator = new VerifyDocumentValidator();
            var result = validator.Validate(dto);
            result.IsValid.Should().BeTrue();
        }

        // ── Upload valid JPG ─────────────────────────────────────────────────

        [Test]
        public async Task UploadDocumentAsync_ValidJpg_ReturnsDocumentResponse()
        {
            var file = CreateMockFile("photo.jpg", 512_000, "image/jpeg");
            var dto = CreateUploadDto();
            dto.DocumentType = DocumentType.PAN;

            _repositoryMock.Setup(r => r.GetByApplicationIdAndTypeAsync(_applicationId, DocumentType.PAN))
                .ReturnsAsync((Document?)null);

            var savedDoc = CreateSampleDocument();
            _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<Document>())).ReturnsAsync(savedDoc);

            var result = await _service.UploadDocumentAsync(file, dto, _userId);

            result.Should().NotBeNull();
            _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Document>()), Times.Once);
        }
    }
}
