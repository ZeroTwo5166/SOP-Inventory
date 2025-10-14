import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { Preset } from '../models/preset';


@Injectable({
  providedIn: 'root',
})


export class PresetService {
  private readonly apiUrl = environment.apiUrl + 'Preset';

  currentUser: any;

  constructor(private http: HttpClient, private authService: AuthService) {
    this.currentUser = this.authService.currentUserValue;
  }

  //* Method for getting all presets. \\
  getAll(): Observable<Preset[]> {
    return this.http.get<Preset[]>(this.apiUrl);
  }

  //* Method for getting a preset by ID. \\
  findById(presetId: number): Observable<Preset> {
    return this.http.get<Preset>(`${this.apiUrl}/${presetId}`);
  }

  //* Method for creating a preset. \\
  create(preset: Preset): Observable<Preset> {
    return this.http.post<Preset>(this.apiUrl, preset);
  }

  //* Method for deleting a preset by ID. \\
  delete(Id: number): Observable<Preset> {
    return this.http.delete<Preset>(this.apiUrl + '/' + Id);
  }
  


}