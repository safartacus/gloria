using Gloria.Domain.Common;

namespace Gloria.Domain.Imports;

public static class ImportErrorCodes
{
    public const string EmployeeNotFound = "EMPLOYEE_NOT_FOUND";
    public const string InvalidAmount = "INVALID_AMOUNT";
    public const string InvalidDate = "INVALID_DATE";
    public const string RequiredFieldMissing = "REQUIRED_FIELD_MISSING";
    public const string Duplicate = "DUPLICATE";
    public const string UnknownRecordType = "UNKNOWN_RECORD_TYPE";
    public const string UnpostedRecord = "UNPOSTED_RECORD";
    public const string ParseError = "PARSE_ERROR";
    public const string PeriodClosed = "PERIOD_CLOSED";
    public const string SourceNotRegistered = "SOURCE_NOT_REGISTERED";
}

/// <summary>Quarantine record for a raw row that could not be imported. Good rows keep flowing.</summary>
public sealed class ImportError : Entity<long>
{
    public int ImportBatchId { get; private set; }
    public string SourceSystemCode { get; private set; } = default!;
    public int RowNumber { get; private set; }
    public string RawContent { get; private set; } = default!;
    public string ErrorCode { get; private set; } = default!;
    public string ErrorMessage { get; private set; } = default!;
    public DateTime TimestampUtc { get; private set; }

    private ImportError() { }

    internal ImportError(string sourceSystemCode, int rowNumber, string rawContent, string errorCode, string errorMessage, DateTime now)
    {
        SourceSystemCode = sourceSystemCode;
        RowNumber = rowNumber;
        RawContent = rawContent;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        TimestampUtc = now;
    }
}
