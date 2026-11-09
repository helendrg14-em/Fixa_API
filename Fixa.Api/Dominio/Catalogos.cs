namespace Fixa.Api.Dominio;

/// <summary>Prioridad que fija el supervisor (RN-05); el ciudadano no la elige.</summary>
public enum Prioridad { Critica, Alta, Media, Baja }

/// <summary>Por dónde llegó el reporte. Los canales asistidos son versión futura (RF-03).</summary>
public enum CanalOrigen { AppMovil, Web, Telefono, Presencial }

/// <summary>Perfiles de funcionario. El operador es versión futura y no entra al panel.</summary>
public enum TipoUsuario { Administrador, Supervisor, Tecnico, Operador }

public enum TipoEvento { Registro, CambioEstado, Priorizacion, Programacion, Derivacion, Validacion, Vinculacion, Intervencion }

/// <summary>Evidencia del trabajo (RF-09): foto, o acta técnica justificada cuando no se puede fotografiar.</summary>
public enum TipoEvidencia { Fotografia, ActaTecnica }

public enum EstadoResolucion { Pendiente, Aprobada, Rechazada }

public enum CanalNotificacion { Push, Correo }

public enum EstadoEntrega { Pendiente, Enviada, NoEntregada }

/// <summary>Roles tal como viajan en el token JWT (mismo texto que la columna usuarios.tipo).</summary>
public static class Roles
{
    public const string Administrador = "administrador";
    public const string Supervisor = "supervisor";
    public const string Tecnico = "tecnico";
}
