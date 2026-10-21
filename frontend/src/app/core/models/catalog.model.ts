export interface Country {
  id: string;
  code: string;
  name: string;
}

export interface Department {
  id: string;
  country_id: string;
  code: string;
  name: string;
}

export interface Municipality {
  id: string;
  department_id: string;
  code: string;
  name: string;
}

export interface Currency {
  id: string;
  iso_code: string;
  name: string;
}

export interface DocumentType {
  id: string;
  code: string;
  name: string;
}
