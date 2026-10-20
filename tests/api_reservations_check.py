"""Regressões de reservas agora usam o banco descartável do harness integrado."""
from api_negotiation_check import run_http
if __name__=='__main__':
    run_http('all')
