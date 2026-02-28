using Bangkok.Application.Dto.Permissions;
using Bangkok.Application.Dto.Roles;
using Bangkok.Application.Interfaces;
using Bangkok.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Bangkok.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
[EnableRateLimiting("GlobalPolicy")]
[SwaggerTag("Role management. Admin only.")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService, IPermissionService permissionService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _permissionService = permissionService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "List roles", Description = "Get all roles. Admin only.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleResponse>>>> GetRoles(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var roles = await _roleService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<IReadOnlyList<RoleResponse>>.Ok(roles, correlationId));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Get role by ID", Description = "Get a single role. Admin only.")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> GetRole([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var role = await _roleService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (role == null)
            return NotFound(ApiResponse<RoleResponse>.Fail(new ErrorResponse { Code = "ROLE_NOT_FOUND", Message = "Role not found." }, correlationId));
        return Ok(ApiResponse<RoleResponse>.Ok(role, correlationId));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Create role", Description = "Create a new role. Admin only.")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<RoleResponse>.Fail(new ErrorResponse { Code = "BAD_REQUEST", Message = "Role name is required." }, correlationId));

        var role = await _roleService.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        if (role == null)
            return BadRequest(ApiResponse<RoleResponse>.Fail(new ErrorResponse { Code = "ROLE_EXISTS", Message = "A role with this name already exists." }, correlationId));

        _logger.LogInformation("Role created. RoleId: {RoleId}, Name: {Name}", role.Id, role.Name);
        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, ApiResponse<RoleResponse>.Ok(role, correlationId));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Update role", Description = "Update a role. Admin only.")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> UpdateRole([FromRoute] Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        if (request == null)
            return BadRequest(ApiResponse<RoleResponse>.Fail(new ErrorResponse { Code = "BAD_REQUEST", Message = "Request body is required." }, correlationId));

        var role = await _roleService.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
        if (role == null)
            return NotFound(ApiResponse<RoleResponse>.Fail(new ErrorResponse { Code = "ROLE_NOT_FOUND", Message = "Role not found or duplicate name." }, correlationId));

        _logger.LogInformation("Role updated. RoleId: {RoleId}, Name: {Name}", role.Id, role.Name);
        return Ok(ApiResponse<RoleResponse>.Ok(role, correlationId));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Delete role", Description = "Delete a role and remove all user assignments. Admin only.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var deleted = await _roleService.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        if (!deleted)
            return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "ROLE_NOT_FOUND", Message = "Role not found." }, correlationId));

        _logger.LogInformation("Role deleted. RoleId: {RoleId}", id);
        return NoContent();
    }

    [HttpGet("{id:guid}/permissions")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Get permissions for role", Description = "Get all permissions assigned to a role. Admin only.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionResponse>>>> GetRolePermissions([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var permissions = await _permissionService.GetByRoleIdAsync(id, cancellationToken).ConfigureAwait(false);
        return Ok(ApiResponse<IReadOnlyList<PermissionResponse>>.Ok(permissions, correlationId));
    }

    [HttpPost("{id:guid}/permissions/{permissionId:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Assign permission to role", Description = "Assign a permission to a role. Admin only.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPermissionToRole([FromRoute] Guid id, [FromRoute] Guid permissionId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var assigned = await _permissionService.AssignToRoleAsync(id, permissionId, cancellationToken).ConfigureAwait(false);
        if (!assigned)
            return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "ROLE_OR_PERMISSION_NOT_FOUND", Message = "Role or permission not found." }, correlationId));
        return NoContent();
    }

    [HttpDelete("{id:guid}/permissions/{permissionId:guid}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "Remove permission from role", Description = "Remove a permission from a role. Admin only.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermissionFromRole([FromRoute] Guid id, [FromRoute] Guid permissionId, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var removed = await _permissionService.RemoveFromRoleAsync(id, permissionId, cancellationToken).ConfigureAwait(false);
        if (!removed)
            return NotFound(ApiResponse<object>.Fail(new ErrorResponse { Code = "ROLE_OR_PERMISSION_NOT_FOUND", Message = "Role or permission not found." }, correlationId));
        return NoContent();
    }
}
