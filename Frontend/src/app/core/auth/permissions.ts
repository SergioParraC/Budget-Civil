/** Permisos declarados en el backend (`AuthorizationPolicies` / seeder de seguridad). */
export const Permission = {
  apuPublicRead: 'APU_PUBLIC_READ',
  catalogsRead: 'CATALOGS_READ',
  usersRead: 'USERS_READ',
  usersCreate: 'USERS_CREATE',
  usersUpdate: 'USERS_UPDATE',
  usersDeactivate: 'USERS_DEACTIVATE',
  usersRoleChange: 'USERS_ROLE_CHANGE',
  usersCompanyChange: 'USERS_COMPANY_CHANGE',
  companiesCreate: 'COMPANIES_CREATE',
  companiesUpdate: 'COMPANIES_UPDATE',
  companiesDeactivate: 'COMPANIES_DEACTIVATE',
  companiesReactivate: 'COMPANIES_REACTIVATE',
  companiesRead: 'COMPANIES_READ',
  companyProfileReadSelf: 'COMPANY_PROFILE_READ_SELF',
  companyLeaveSelf: 'COMPANY_LEAVE_SELF',
  companyMembersAssign: 'COMPANY_MEMBERS_ASSIGN',
  companyMembersDeactivate: 'COMPANY_MEMBERS_DEACTIVATE',
  projectsCreate: 'PROJECTS_CREATE',
  projectsUpdate: 'PROJECTS_UPDATE',
  projectsStatusChange: 'PROJECTS_STATUS_CHANGE',
  projectsVisibilityChange: 'PROJECTS_VISIBILITY_CHANGE',
  projectsRead: 'PROJECTS_READ',
  projectMembersAssign: 'PROJECT_MEMBERS_ASSIGN',
  projectMembersDeactivate: 'PROJECT_MEMBERS_DEACTIVATE',
  profileReadSelf: 'PROFILE_READ_SELF',
  profileUpdateSelf: 'PROFILE_UPDATE_SELF',
  profileChangePasswordSelf: 'PROFILE_CHANGE_PASSWORD_SELF'
} as const;

export type PermissionKey = (typeof Permission)[keyof typeof Permission];
