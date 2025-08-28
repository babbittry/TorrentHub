namespace TorrentHub.Core.Enums;

public enum UserRole
{
    // Standard User Tiers (increasing privileges)
    Mosquito = 0,   // �ͷ������û�
    User,           // ��ͨ�û� (��ע���û���Ĭ�Ͻ�ɫ)
    PowerUser,      // �߼��û�
    EliteUser,      // ��Ӣ�û�
    CrazyUser,      // �����û�
    VeteranUser,    // �����û�
    VIP,            // VIP�û������磬�����߻����⹱���ߣ�

    // Functional Roles
    Uploader,       // ��֤�ϴ���
    Seeder,         // �����û�
    

    // Staff Roles
    Moderator,      // ����
    Administrator   // ����Ա
}
