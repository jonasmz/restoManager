namespace RestoManager.Business.Domain.Common;

/// <summary>Errores esperables del dominio. La capa Api los mapea a <c>ProblemDetails</c>.</summary>
public abstract class BusinessException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;

    /// <summary>Status HTTP sugerido para la Api.</summary>
    public abstract int StatusCode { get; }
}

/// <summary>Regla de negocio violada (409).</summary>
public sealed class DomainRuleException(string code, string message)
    : BusinessException(code, message)
{
    public override int StatusCode => 409;
}

/// <summary>Entrada del cliente mal formada o fuera de rango (400).</summary>
public sealed class InvalidInputException(string code, string message)
    : BusinessException(code, message)
{
    public override int StatusCode => 400;
}

/// <summary>Recurso no encontrado (404).</summary>
public sealed class NotFoundException(string resource, object key)
    : BusinessException("not_found", $"No se encontró {resource} con clave '{key}'.")
{
    public override int StatusCode => 404;
}

/// <summary>El empleado del token no está habilitado para esa sucursal — DOM-06 (403).</summary>
public sealed class BranchAccessDeniedException(int branchId)
    : BusinessException("branch_access_denied", $"No autorizado para operar en la sucursal {branchId}.")
{
    public override int StatusCode => 403;
}
