namespace FeaSolution.Core.Exceptions;

/// <summary>
/// Generates Model builder exception.
/// </summary>
public class FeaModelBuilderException(string message) : FeaCommonException(message);