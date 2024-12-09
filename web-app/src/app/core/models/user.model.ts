export interface User {
  id?: number;
  name: string;
  email: string;
  role: Role;
  userId: number;
  photo: string | null;
}

export enum Role {
  Admin = 1,
  User = 2,
}
