export interface CreateUserCommand {
  fullName: string,
  email: string,
  role: Role
}
export enum Role {
  Admin = 1,
  User = 2
}
