using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

// �¼���Ӧ�սӿڣ�����֧�ֿ��п��޵Ĳ�������
public interface IEventInfo
{ }

//�����¼���Ӧ
public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }
}

//�޲��¼���Ӧ
public class EventInfo : IEventInfo
{
    public UnityAction actions;

    public EventInfo(UnityAction action)
    {
        actions += action;
    }
}

// �¼�����
// ����ע�ᣨ�������¼����ַ����������¼�
// �¼�֧�� ������ �� �޲��� ����
// �������¼�ʹ�� EventInfo<T> ��������
public class EventCenter
{
    private Dictionary<string, IEventInfo> _eventDic = new Dictionary<string, IEventInfo>();
    private static EventCenter instance;

    //����
    public static EventCenter Instance
    {
        get 
        {
            if (instance == null)
            {
                instance= new EventCenter();
            }
            return instance;
        }
    }

    //��Ӵ������¼��ļ���
    public void AddEventListener<T>(string name, UnityAction<T> action)
    {
        //���¼�
        if (_eventDic.ContainsKey(name))
            (_eventDic[name] as EventInfo<T>).actions += action;
        else
            _eventDic.Add(name, new EventInfo<T>(action));
    }

    //����޲����¼��ļ���
    public void AddEventListener(string name, UnityAction action)
    {
        //���¼�
        if (_eventDic.ContainsKey(name))
            (_eventDic[name] as EventInfo).actions += action;
        else
            _eventDic.Add(name, new EventInfo(action));
    }

    //�Ƴ������¼��ļ���
    public void RemoveEventListener<T>(string name, UnityAction<T> action)
    {
        if (_eventDic.ContainsKey(name))
            (_eventDic[name] as EventInfo<T>).actions -= action;
    }

    //�Ƴ��޲��¼��ļ���
    public void RemoveEventListener(string name, UnityAction action)
    {
        if (_eventDic.ContainsKey(name))
            (_eventDic[name] as EventInfo).actions -= action;
    }

    //�ַ����������¼�
    public void EventTrigger<T>(string name, T info)
    {
        if (_eventDic.ContainsKey(name))
        {
            if ((_eventDic[name] as EventInfo<T>).actions != null)
                (_eventDic[name] as EventInfo<T>).actions.Invoke(info);
        }
    }

    // �ַ��޲������¼�
    public void EventTrigger(string name)
    {
        if (_eventDic.ContainsKey(name))
            if ((_eventDic[name] as EventInfo).actions != null)
                (_eventDic[name] as EventInfo).actions.Invoke();
    }

    // ����¼�����
    // ��Ҫ���ڳ����л�ʱ��ֹ�ڴ�й©
    public void Clear()
    {
        _eventDic.Clear();
    }

}
