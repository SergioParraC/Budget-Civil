export interface Country {
  id: string;
  code: string;
  name: string;
}

export interface Department {
  id: string;
  countryId: string;
  code: string;
  name: string;
}

export interface Municipality {
  id: string;
  departmentId: string;
  code: string;
  name: string;
}

export interface Currency {
  id: string;
  isoCode: string;
  name: string;
}

export interface DocumentType {
  id: string;
  code: string;
  name: string;
}
