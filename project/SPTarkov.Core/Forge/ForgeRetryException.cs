namespace SPTarkov.Core.Forge;

public class ForgeRetryException(string? message, Exception? innerException) : Exception(message, innerException);
