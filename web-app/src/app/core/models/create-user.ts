export interface CreateUserCommand {
  fullName: string,
  email: string,
  role: Role
}
export enum Role {
  Admin = 1,
  User = 2
}
export type UpdateUserProfileCommand = {
  userId: number,
  currentPassword: string,
  newPassword: string,
  confirmNewPassword: string,
}
