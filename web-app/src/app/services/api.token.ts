import { InjectionToken } from "@angular/core";
import { environment } from "src/environments/environment.development";

export const API_URL = new InjectionToken('API BASE URL', {
  factory: () => environment.apiUrl,
});
