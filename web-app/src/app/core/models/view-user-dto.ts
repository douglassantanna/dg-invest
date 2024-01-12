import { Role } from "./create-user";

export interface ViewUserDto {
  id: number;
  fullName: string;
  email: string;
  role: Role;
}
