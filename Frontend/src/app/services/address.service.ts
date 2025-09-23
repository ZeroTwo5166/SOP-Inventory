import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Address } from '../models/address';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root',
})
export class AddressService {
  //* The URL to the API endpoint for addresses. \\
  private readonly apiUrl = environment.apiUrl + 'address';

  //* getting the current user from the  AuthService\\
  currentUser: any;

  constructor(private http: HttpClient, private authService: AuthService) {
    this.currentUser = this.authService.currentUserValue;
  }

  //* Method for getting all addresses. \\
  getAll(): Observable<Address[]> {
    return this.http.get<Address[]>(this.apiUrl);
  }

  //* Method for getting an address by ID. \\
  findById(Id: number): Observable<Address> {
    return this.http.get<Address>(this.apiUrl + '/' + Id);
  }

  //* Method for creating an address. \\
  create(address: Address): Observable<Address> {
    return this.http.post<Address>(this.apiUrl, address);
  }

  //* Method for deleting an address by ID. \\
  delete(Id: number): Observable<Address> {
    return this.http.delete<Address>(this.apiUrl + '/' + Id);
  }

  //* Method for updating an address. \\
  update(address: Address): Observable<Address> {
    return this.http.put<Address>(
      this.apiUrl + '/' + address.id, address);
  }
}
