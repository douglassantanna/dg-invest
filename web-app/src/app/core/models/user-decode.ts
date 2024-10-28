import { Role } from "./user.model";

export interface UserDecode {
  unique_name: string;
  email: string;
  role: Role;
  nameid: string;
}
