using System;

namespace MobileWalletProtocol
{
    public static class ErrorUtils
    {
        private const string FallbackMessage = "Unspecified error message.";
        public const string JsonRpcServerErrorMessage = "Unspecified server error.";

        public static string GetMessageFromCode(int? code, string fallbackMessage = FallbackMessage)
        {
            if (code.HasValue && ErrorValues.GetValue(code.Value) is ErrorValue errorValue)
            {
                return errorValue.Message;
            }

            if (code.HasValue && IsJsonRpcServerError(code.Value))
            {
                return JsonRpcServerErrorMessage;
            }

            return fallbackMessage;
        }

        public static bool IsValidCode(int code)
        {
            return ErrorValues.GetValue(code) != null || IsJsonRpcServerError(code);
        }

        public static int? GetErrorCode(object error)
        {
            return error switch
            {
                int intError => intError,
                IErrorWithCode errorWithCode => errorWithCode.Code ?? errorWithCode.ErrorCode,
                _ => null
            };
        }

        private static bool IsJsonRpcServerError(int code)
        {
            return code >= -32099 && code <= -32000;
        }
    }

    public interface IErrorWithCode
    {
        int? Code { get; }
        int? ErrorCode { get; }
    }

    public class SerializedEthereumRpcError
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public string Stack { get; set; }

        public static SerializedEthereumRpcError Serialize(object error, bool shouldIncludeStack = false)
        {
            var serialized = new SerializedEthereumRpcError();

            if (error is SerializedEthereumRpcError rpcError && ErrorUtils.IsValidCode(rpcError.Code))
            {
                serialized.Code = rpcError.Code;
                if (!string.IsNullOrEmpty(rpcError.Message))
                {
                    serialized.Message = rpcError.Message;
                    serialized.Data = rpcError.Data;
                }
                else
                {
                    serialized.Message = ErrorUtils.GetMessageFromCode(serialized.Code);
                    serialized.Data = new { originalError = AssignOriginalError(error) };
                }
            }
            else
            {
                serialized.Code = StandardErrorCodes.Rpc.Internal;
                serialized.Message = error is Exception ex ? ex.Message : FallbackMessage;
                serialized.Data = new { originalError = AssignOriginalError(error) };
            }

            if (shouldIncludeStack && error is Exception exception)
            {
                serialized.Stack = exception.StackTrace;
            }

            return serialized;
        }

        private static object AssignOriginalError(object error)
        {
            if (error is Exception)
            {
                return new
                {
                    message = ((Exception)error).Message,
                    stack = ((Exception)error).StackTrace
                };
            }
            return error;
        }

        private const string FallbackMessage = "Unspecified error message.";
    }
}