import { Role } from "./user.model";

export interface NavItems {
  label: string,
  path: string,
  icon: string,
  roles: Role[]
}
