
using util;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using CrimeAnalysisAndReportingSystem.dao;
using CrimeAnalysisAndReportingSystem.entity;
using CrimeAnalysisAndReportingSystem.exception;

namespace dao
{
    public class CrimeAnalysisServiceImpl : ICrimeAnalysisService
    {
        private SqlConnection connection;

        public CrimeAnalysisServiceImpl()
        {
            connection = DBConnUtil.GetConnection();
        }

        public bool CreateIncident(Incident incident)
        {
            try
            {
                string query = @"INSERT INTO Incidents (IncidentType, IncidentDate, Location, Description, Status, VictimID, SuspectID)
                                 VALUES (@type, @date, @location, @desc, @status, @victimId, @suspectId)";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@type", incident.IncidentType);
                cmd.Parameters.AddWithValue("@date", incident.IncidentDate);
                cmd.Parameters.AddWithValue("@location", incident.Location);
                cmd.Parameters.AddWithValue("@desc", incident.Description);
                cmd.Parameters.AddWithValue("@status", incident.Status);
                cmd.Parameters.AddWithValue("@victimId", incident.VictimID);
                cmd.Parameters.AddWithValue("@suspectId", incident.SuspectID);

                connection.Open();
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating incident: " + ex.Message);
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        public bool UpdateIncidentStatus(int incidentId, string newStatus)
        {
            try
            {
                string query = "UPDATE Incidents SET Status = @status WHERE IncidentID = @id";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@status", newStatus);
                cmd.Parameters.AddWithValue("@id", incidentId);

                connection.Open();
                int rows = cmd.ExecuteNonQuery();

                if (rows == 0)
                    throw new IncidentNumberNotFoundException("Incident ID not found!");

                return true;
            }
            catch (IncidentNumberNotFoundException ex)
            {
                Console.WriteLine("Custom Exception: " + ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating status: " + ex.Message);
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        public List<Incident> GetIncidentsInDateRange(DateTime startDate, DateTime endDate)
        {
            List<Incident> incidents = new List<Incident>();
            try
            {
                string query = "SELECT * FROM Incidents WHERE IncidentDate BETWEEN @start AND @end";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@start", startDate);
                cmd.Parameters.AddWithValue("@end", endDate);

                connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    incidents.Add(new Incident
                    {
                        IncidentID = (int)reader["IncidentID"],
                        IncidentType = reader["IncidentType"].ToString(),
                        IncidentDate = Convert.ToDateTime(reader["IncidentDate"]),
                        Location = reader["Location"].ToString(),
                        Description = reader["Description"].ToString(),
                        Status = reader["Status"].ToString(),
                        VictimID = (int)reader["VictimID"],
                        SuspectID = (int)reader["SuspectID"]
                    });
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error fetching incidents: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
            return incidents;
        }

        public List<Incident> SearchIncidents(string incidentType)
        {
            List<Incident> incidents = new List<Incident>();
            try
            {
                string query = "SELECT * FROM Incidents WHERE IncidentType = @type";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@type", incidentType);

                connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    incidents.Add(new Incident
                    {
                        IncidentID = (int)reader["IncidentID"],
                        IncidentType = reader["IncidentType"].ToString(),
                        IncidentDate = Convert.ToDateTime(reader["IncidentDate"]),
                        Location = reader["Location"].ToString(),
                        Description = reader["Description"].ToString(),
                        Status = reader["Status"].ToString(),
                        VictimID = (int)reader["VictimID"],
                        SuspectID = (int)reader["SuspectID"]
                    });
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error searching incidents: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
            return incidents;
        }

        public Report GenerateIncidentReport(Incident incident)
        {
            try
            {
                return new Report
                {
                    IncidentID = incident.IncidentID,
                    ReportingOfficerID = 1, // Hardcoded for example
                    ReportDate = DateTime.Now,
                    ReportDetails = $"Report on incident {incident.IncidentID}: {incident.Description}",
                    Status = "Draft"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating report: " + ex.Message);
                return null;
            }
        }

        public bool CreateCase(string caseDescription, List<Incident> incidents)
        {
            try
            {
                string query = "INSERT INTO Cases (CaseDescription) OUTPUT INSERTED.CaseID VALUES (@desc)";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@desc", caseDescription);

                connection.Open();
                int caseId = (int)cmd.ExecuteScalar();

                foreach (var incident in incidents)
                {
                    string mapQuery = "INSERT INTO CaseIncident (CaseID, IncidentID) VALUES (@caseId, @incidentId)";
                    SqlCommand mapCmd = new SqlCommand(mapQuery, connection);
                    mapCmd.Parameters.AddWithValue("@caseId", caseId);
                    mapCmd.Parameters.AddWithValue("@incidentId", incident.IncidentID);
                    mapCmd.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating case: " + ex.Message);
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        public Case GetCaseDetails(int caseId)
        {
            Case caseObj = null;
            try
            {
                string query = "SELECT * FROM Cases WHERE CaseID = @id";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@id", caseId);

                connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    caseObj = new Case
                    {
                        CaseID = (int)reader["CaseID"],
                        Description = reader["CaseDescription"].ToString(),
                        Incidents = new List<Incident>()
                    };
                }
                reader.Close();

                if (caseObj != null)
                {
                    string mapQuery = "SELECT i.* FROM Incidents i JOIN CaseIncident ci ON i.IncidentID = ci.IncidentID WHERE ci.CaseID = @caseId";
                    SqlCommand mapCmd = new SqlCommand(mapQuery, connection);
                    mapCmd.Parameters.AddWithValue("@caseId", caseId);
                    SqlDataReader incidentReader = mapCmd.ExecuteReader();
                    while (incidentReader.Read())
                    {
                        caseObj.Incidents.Add(new Incident
                        {
                            IncidentID = (int)incidentReader["IncidentID"],
                            IncidentType = incidentReader["IncidentType"].ToString(),
                            IncidentDate = Convert.ToDateTime(incidentReader["IncidentDate"]),
                            Location = incidentReader["Location"].ToString(),
                            Description = incidentReader["Description"].ToString(),
                            Status = incidentReader["Status"].ToString(),
                            VictimID = (int)incidentReader["VictimID"],
                            SuspectID = (int)incidentReader["SuspectID"]
                        });
                    }
                    incidentReader.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving case details: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
            return caseObj;
        }

        public bool UpdateCaseDetails(Case caseDetails)
        {
            try
            {
                string query = "UPDATE Cases SET CaseDescription = @desc WHERE CaseID = @id";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@desc", caseDetails.Description);
                cmd.Parameters.AddWithValue("@id", caseDetails.CaseID);

                connection.Open();
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error updating case: " + ex.Message);
                return false;
            }
            finally
            {
                connection.Close();
            }
        }

        public List<Case> GetAllCases()
        {
            List<Case> cases = new List<Case>();
            try
            {
                string query = "SELECT * FROM Cases";
                SqlCommand cmd = new SqlCommand(query, connection);

                connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    cases.Add(new Case
                    {
                        CaseID = (int)reader["CaseID"],
                        Description = reader["CaseDescription"].ToString(),
                        Incidents = new List<Incident>()
                    });
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error getting all cases: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
            return cases;
        }
    }
}
