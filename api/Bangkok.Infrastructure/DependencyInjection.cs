using Bangkok.Application.Configuration;
using Bangkok.Application.Interfaces;
using Bangkok.Infrastructure.Data;
using Bangkok.Infrastructure.Repositories;
using Bangkok.Infrastructure.Security;
using Bangkok.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bangkok.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SqlConnectionOptions>(options =>
        {
            options.DefaultConnection = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        });
        services.Configure<AttachmentSettings>(configuration.GetSection(AttachmentSettings.SectionName));
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectTemplateRepository, ProjectTemplateRepository>();
        services.AddScoped<IProjectTemplateTaskRepository, ProjectTemplateTaskRepository>();
        services.AddScoped<IProjectDashboardRepository, ProjectDashboardRepository>();
        services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<IProjectCustomFieldRepository, ProjectCustomFieldRepository>();
        services.AddScoped<IProjectAutomationRuleRepository, ProjectAutomationRuleRepository>();
        services.AddScoped<ITaskCustomFieldValueRepository, TaskCustomFieldValueRepository>();
        services.AddScoped<ITaskLabelRepository, TaskLabelRepository>();
        services.AddScoped<ITaskCommentRepository, TaskCommentRepository>();
        services.AddScoped<ITaskActivityRepository, TaskActivityRepository>();
        services.AddScoped<ITaskTimeLogRepository, TaskTimeLogRepository>();
        services.AddScoped<ITaskAttachmentRepository, TaskAttachmentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserPermissionChecker, UserPermissionChecker>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectTemplateService, ProjectTemplateService>();
        services.AddScoped<IProjectDashboardService, ProjectDashboardService>();
        services.AddScoped<IProjectMemberService, ProjectMemberService>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddScoped<IProjectCustomFieldService, ProjectCustomFieldService>();
        services.AddScoped<IProjectExportService, ProjectExportService>();
        services.AddScoped<IProjectAutomationRuleService, ProjectAutomationRuleService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskCommentService, TaskCommentService>();
        services.AddScoped<ITaskActivityService, TaskActivityService>();
        services.AddScoped<ITaskTimeLogService, TaskTimeLogService>();
        services.AddScoped<IAttachmentFileStorage, LocalAttachmentFileStorage>();
        services.AddScoped<ITaskAttachmentService, TaskAttachmentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddSingleton<IAuditLogger, AuditLogger>();
        services.AddSingleton<RoleSeedRunner>();

        return services;
    }
}
