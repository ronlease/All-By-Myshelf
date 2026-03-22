// Feature: Input sanitization and validation utilities (ABM-068)
//
// Scenario: Sanitize returns empty string for null input
//   Given the input is null
//   When Sanitize is called
//   Then an empty string is returned
//
// Scenario: Sanitize returns empty string for whitespace-only input
//   Given the input is whitespace-only
//   When Sanitize is called
//   Then an empty string is returned
//
// Scenario: Sanitize trims leading and trailing whitespace
//   Given the input has leading and trailing whitespace
//   When Sanitize is called
//   Then the whitespace is trimmed
//
// Scenario: Sanitize strips control characters
//   Given the input contains control characters like null byte and backspace
//   When Sanitize is called
//   Then control characters are removed
//
// Scenario: Sanitize preserves newlines when preserveNewlines is true
//   Given the input contains newline characters
//   When Sanitize is called with preserveNewlines: true
//   Then newlines are preserved
//
// Scenario: Sanitize strips newlines when preserveNewlines is false
//   Given the input contains newline characters
//   When Sanitize is called with preserveNewlines: false (default)
//   Then newlines are removed
//
// Scenario: Sanitize truncates to maxLength
//   Given the input is longer than maxLength
//   When Sanitize is called with a specific maxLength
//   Then the result is truncated to maxLength
//
// Scenario: Sanitize handles string shorter than maxLength
//   Given the input is shorter than maxLength
//   When Sanitize is called with a specific maxLength
//   Then the full input is returned without truncation
//
// Scenario: Redact returns "[empty]" for null input
//   Given the input is null
//   When Redact is called
//   Then "[empty]" is returned
//
// Scenario: Redact returns "[empty]" for whitespace-only input
//   Given the input is whitespace-only
//   When Redact is called
//   Then "[empty]" is returned
//
// Scenario: Redact returns "***" when input is shorter than visibleChars
//   Given the input is shorter than the visibleChars parameter
//   When Redact is called
//   Then "***" is returned
//
// Scenario: Redact returns first N chars plus "***" for normal input
//   Given the input is longer than visibleChars
//   When Redact is called
//   Then the first N chars are shown followed by "***"
//
// Scenario: Redact custom visibleChars works correctly
//   Given a custom visibleChars value is specified
//   When Redact is called
//   Then the specified number of chars are shown followed by "***"
//
// Scenario: SplitNames returns empty list for null input
//   Given the input is null
//   When SplitNames is called
//   Then an empty list is returned
//
// Scenario: SplitNames returns empty list for whitespace-only input
//   Given the input is whitespace-only
//   When SplitNames is called
//   Then an empty list is returned
//
// Scenario: SplitNames splits comma-separated names correctly
//   Given the input contains comma-separated names
//   When SplitNames is called
//   Then a list of individual names is returned
//
// Scenario: SplitNames trims whitespace from each name
//   Given the input has names with leading/trailing whitespace
//   When SplitNames is called
//   Then each name is trimmed
//
// Scenario: SplitNames excludes empty or whitespace-only segments
//   Given the input has empty or whitespace-only segments
//   When SplitNames is called
//   Then those segments are excluded from the result
//
// Scenario: SplitNames handles single name with no comma
//   Given the input is a single name with no comma
//   When SplitNames is called
//   Then a list with one name is returned
//
// Scenario: EscapeLikePattern returns empty string for null input
//   Given the input is null
//   When EscapeLikePattern is called
//   Then an empty string is returned
//
// Scenario: EscapeLikePattern returns empty string for whitespace input
//   Given the input is whitespace-only
//   When EscapeLikePattern is called
//   Then an empty string is returned
//
// Scenario: EscapeLikePattern escapes % character
//   Given the input contains %
//   When EscapeLikePattern is called
//   Then % is escaped to \%
//
// Scenario: EscapeLikePattern escapes _ character
//   Given the input contains _
//   When EscapeLikePattern is called
//   Then _ is escaped to \_
//
// Scenario: EscapeLikePattern escapes \ character
//   Given the input contains \
//   When EscapeLikePattern is called
//   Then \ is escaped to \\
//
// Scenario: EscapeLikePattern leaves normal characters unchanged
//   Given the input contains only normal characters
//   When EscapeLikePattern is called
//   Then the output is unchanged
//
// Scenario: EscapeLikePattern handles multiple special characters in one string
//   Given the input contains multiple special characters
//   When EscapeLikePattern is called
//   Then all special characters are escaped correctly

using AllByMyshelf.Api.Infrastructure;
using FluentAssertions;

namespace AllByMyshelf.Unit.Infrastructure;

public class InputSanitizerTests
{
    // ── EscapeLikePattern ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EscapeLikePattern_NullOrWhitespace_ReturnsEmptyString(string? input)
    {
        // Act
        var result = InputSanitizer.EscapeLikePattern(input!);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void EscapeLikePattern_PercentCharacter_IsEscaped()
    {
        // Arrange
        var input = "test%value";

        // Act
        var result = InputSanitizer.EscapeLikePattern(input);

        // Assert
        result.Should().Be("test\\%value");
    }

    [Fact]
    public void EscapeLikePattern_UnderscoreCharacter_IsEscaped()
    {
        // Arrange
        var input = "test_value";

        // Act
        var result = InputSanitizer.EscapeLikePattern(input);

        // Assert
        result.Should().Be("test\\_value");
    }

    [Fact]
    public void EscapeLikePattern_BackslashCharacter_IsEscaped()
    {
        // Arrange
        var input = "test\\value";

        // Act
        var result = InputSanitizer.EscapeLikePattern(input);

        // Assert
        result.Should().Be("test\\\\value");
    }

    [Fact]
    public void EscapeLikePattern_NormalCharacters_RemainsUnchanged()
    {
        // Arrange
        var input = "normal text 123";

        // Act
        var result = InputSanitizer.EscapeLikePattern(input);

        // Assert
        result.Should().Be("normal text 123");
    }

    [Fact]
    public void EscapeLikePattern_MultipleSpecialCharacters_AllEscaped()
    {
        // Arrange
        var input = "test\\with%all_special";

        // Act
        var result = InputSanitizer.EscapeLikePattern(input);

        // Assert
        result.Should().Be("test\\\\with\\%all\\_special");
    }

    // ── Redact ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Redact_NullOrWhitespace_ReturnsEmpty(string? input)
    {
        // Act
        var result = InputSanitizer.Redact(input);

        // Assert
        result.Should().Be("[empty]");
    }

    [Theory]
    [InlineData("abc", 4)]
    [InlineData("1234", 4)]
    [InlineData("x", 2)]
    public void Redact_InputShorterThanVisibleChars_ReturnsStars(string input, int visibleChars)
    {
        // Act
        var result = InputSanitizer.Redact(input, visibleChars);

        // Assert
        result.Should().Be("***");
    }

    [Fact]
    public void Redact_NormalInput_ReturnsFirstCharsAndStars()
    {
        // Arrange
        var input = "supersecret123";

        // Act
        var result = InputSanitizer.Redact(input);

        // Assert
        result.Should().Be("supe***");
    }

    [Fact]
    public void Redact_CustomVisibleChars_ReturnsCorrectPrefix()
    {
        // Arrange
        var input = "mysecretvalue";

        // Act
        var result = InputSanitizer.Redact(input, visibleChars: 6);

        // Assert
        result.Should().Be("mysecr***");
    }

    // ── Sanitize ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sanitize_NullOrWhitespace_ReturnsEmptyString(string? input)
    {
        // Act
        var result = InputSanitizer.Sanitize(input);

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Sanitize_LeadingAndTrailingWhitespace_IsTrimmed()
    {
        // Arrange
        var input = "   test value   ";

        // Act
        var result = InputSanitizer.Sanitize(input);

        // Assert
        result.Should().Be("test value");
    }

    [Fact]
    public void Sanitize_ControlCharacters_AreStripped()
    {
        // Arrange
        var input = "test\0value\bhere";

        // Act
        var result = InputSanitizer.Sanitize(input);

        // Assert
        result.Should().Be("testvaluehere");
    }

    [Fact]
    public void Sanitize_NewlinesWithPreserveTrue_ArePreserved()
    {
        // Arrange
        var input = "line1\nline2\rline3";

        // Act
        var result = InputSanitizer.Sanitize(input, preserveNewlines: true);

        // Assert
        result.Should().Be("line1\nline2\rline3");
    }

    [Fact]
    public void Sanitize_NewlinesWithPreserveFalse_AreStripped()
    {
        // Arrange
        var input = "line1\nline2\rline3";

        // Act
        var result = InputSanitizer.Sanitize(input, preserveNewlines: false);

        // Assert
        result.Should().Be("line1line2line3");
    }

    [Fact]
    public void Sanitize_InputLongerThanMaxLength_IsTruncated()
    {
        // Arrange
        var input = "this is a very long string that exceeds the max length";

        // Act
        var result = InputSanitizer.Sanitize(input, maxLength: 10);

        // Assert
        result.Should().Be("this is a ");
        result.Length.Should().Be(10);
    }

    [Fact]
    public void Sanitize_InputShorterThanMaxLength_IsNotTruncated()
    {
        // Arrange
        var input = "short";

        // Act
        var result = InputSanitizer.Sanitize(input, maxLength: 100);

        // Assert
        result.Should().Be("short");
    }

    // ── SplitNames ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SplitNames_NullOrWhitespace_ReturnsEmptyList(string? input)
    {
        // Act
        var result = InputSanitizer.SplitNames(input!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SplitNames_CommaSeparatedNames_ReturnsListOfNames()
    {
        // Arrange
        var input = "Alice,Bob,Charlie";

        // Act
        var result = InputSanitizer.SplitNames(input);

        // Assert
        result.Should().HaveCount(3);
        result.Should().ContainInOrder("Alice", "Bob", "Charlie");
    }

    [Fact]
    public void SplitNames_NamesWithWhitespace_TrimsEachName()
    {
        // Arrange
        var input = "  Alice  ,  Bob  ,  Charlie  ";

        // Act
        var result = InputSanitizer.SplitNames(input);

        // Assert
        result.Should().HaveCount(3);
        result.Should().ContainInOrder("Alice", "Bob", "Charlie");
    }

    [Fact]
    public void SplitNames_EmptySegments_AreExcluded()
    {
        // Arrange
        var input = "Alice,,Bob,   ,Charlie";

        // Act
        var result = InputSanitizer.SplitNames(input);

        // Assert
        result.Should().HaveCount(3);
        result.Should().ContainInOrder("Alice", "Bob", "Charlie");
    }

    [Fact]
    public void SplitNames_SingleName_ReturnsListWithOneName()
    {
        // Arrange
        var input = "Alice";

        // Act
        var result = InputSanitizer.SplitNames(input);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("Alice");
    }
}
